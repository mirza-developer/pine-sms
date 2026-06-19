using Microsoft.EntityFrameworkCore;
using PineAI.Core.Contracts;
using ZiggyCreatures.Caching.Fusion;
using PineAI.Core.Entities;
using PineAI.Core.Features.Order;
using PineAI.Persistence.Services;
using PineAI.Shared;
using System.Globalization;

namespace PineAI.Persistence.Repositories;

public class OrderRepository : IOrderService
{
    private readonly PineAIDbContext dbContext;
    private readonly IBaleMessengerService baleMessengerService;
    private readonly IFusionCache _cache;

    public OrderRepository(PineAIDbContext dbContext, IBaleMessengerService baleMessengerService, IFusionCache cache)
    {
        this.dbContext = dbContext;
        this.baleMessengerService = baleMessengerService;
        _cache = cache;
    }

    public async Task<NotifyOrderResult> NotifyOrder(NotifyOrderCommand command)
    {
        NotifyOrderResult result = new();

        var phone = command.CustomerPhoneNumber.ToNormalizedPhoneNumber();

        if (phone.Length > 10)
        {
            result.Success = false;
            result.Message = $"شماره تلفن '{command.CustomerPhoneNumber}' معتبر نیست";
            return result;
        }

        var customer = await dbContext.Customer.AsNoTracking().FirstOrDefaultAsync(c => c.PhoneNumber == phone);
       
        if (customer is null)
        {
            customer = new Customer
            {
                PhoneNumber = phone,
                SaveDate = DateTime.Now,
                SaveUserId = "system",
                SaveType = 3 // 3 = API
            };
            dbContext.Customer.Add(customer);

            await dbContext.SaveChangesAsync();

            result.IsNewCustomer = true;
        }

        // 2. Ensure order status exists
        var orderStatusCacheKey = $"order_status_{command.OrderStatusCode}";
        var orderStatus = await _cache.GetOrSetAsync<OrderStatus?>(orderStatusCacheKey,
            async (ctx, ct) =>
            {
                var status = await dbContext.OrderStatus.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Code == command.OrderStatusCode, ct);

                // Do not cache null — invalid codes should re-check the DB every time
                if (status is null)
                    ctx.Options.Duration = TimeSpan.Zero;

                return status;
            });
      
        if (orderStatus is null)
        {
            result.Success = false;
           
            result.Message = $"وضعیت سفارش با کد '{command.OrderStatusCode}' یافت نشد";
            
            return result;
        }

        if (command.OrderCode.StartsWith("wc-"))
        {
            command.OrderCode = command.OrderCode.Replace("wc-", "");
        }

        var orderCacheKey = $"customer_order_{command.OrderCode}";
        var trackCacheKey = $"track_order_{command.OrderCode}";

        CustomerOrder? order = await _cache.GetOrDefaultAsync<CustomerOrder?>(orderCacheKey);
        if (order is null)
            order = await dbContext.CustomerOrder.AsNoTracking().FirstOrDefaultAsync(o => o.OrderCode == command.OrderCode);

        if (order is null)
        {
            order = new CustomerOrder
            {
                OrderCode = command.OrderCode,
                CustomerId = customer.Id,
                OrderStatusId = orderStatus.Id,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };

            dbContext.CustomerOrder.Add(order);

            result.IsNewOrder = true;
        }
        else
        {
            dbContext.CustomerOrder.Attach(order);
            order.OrderStatusId = orderStatus.Id;
            order.UpdatedAt = DateTime.Now;
        }

        await dbContext.SaveChangesAsync();

        // Sync cache after save — evict stale track projection, refresh entity cache
        await _cache.RemoveAsync(trackCacheKey);
        await _cache.SetAsync(orderCacheKey, order, new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromDays(1),
            DistributedCacheDuration = TimeSpan.FromDays(14)
        });

        // 4. Notify the customer via Bale messenger
        //var notificationMessage = $"سفارش شما با کد {command.OrderCode} به وضعیت «{orderStatus.Title}» تغییر کرد.";
        //result.NotificationSent = await baleMessengerService.SendMessageAsync(phone, notificationMessage);

        result.Success = true;

        result.Message = result.IsNewOrder ? "سفارش ثبت شد" : "سفارش به‌روزرسانی شد";
       
        return result;
    }

    public async Task<List<OrderStatus>> GetAllOrderStatuses()
    {
        return await dbContext.OrderStatus.AsNoTracking().OrderBy(s => s.Code).ToListAsync();
    }

    public async Task<UpsertOrderStatusResult> UpsertOrderStatus(UpsertOrderStatusCommand command)
    {
        if (command.Id.HasValue)
        {
            var existing = await dbContext.OrderStatus.FindAsync(command.Id.Value);
            if (existing == null)
                return new UpsertOrderStatusResult { Success = false, Message = "وضعیت سفارش یافت نشد" };

            bool codeConflict = await dbContext.OrderStatus.AnyAsync(s => s.Code == command.Code && s.Id != command.Id.Value);
            if (codeConflict)
                return new UpsertOrderStatusResult { Success = false, Message = "این کد قبلاً استفاده شده است" };

            existing.Code = command.Code;
            existing.Title = command.Title;
            existing.LastChange = DateTime.Now;
        }
        else
        {
            bool codeConflict = await dbContext.OrderStatus.AnyAsync(s => s.Code == command.Code);
            if (codeConflict)
                return new UpsertOrderStatusResult { Success = false, Message = "این کد قبلاً استفاده شده است" };

            dbContext.OrderStatus.Add(new OrderStatus
            {
                Code = command.Code,
                Title = command.Title,
                LastChange = DateTime.Now
            });
        }

        await dbContext.SaveChangesAsync();
        await _cache.RemoveAsync($"order_status_{command.Code}");
        return new UpsertOrderStatusResult { Success = true, Message = command.Id.HasValue ? "وضعیت سفارش به‌روزرسانی شد" : "وضعیت سفارش ثبت شد" };
    }

    public async Task<DeleteOrderStatusResult> DeleteOrderStatus(int id)
    {
        var status = await dbContext.OrderStatus.FindAsync(id);

        if (status is null)
        {
            return new DeleteOrderStatusResult { Success = false, Message = "وضعیت سفارش یافت نشد" };
        }

        bool hasOrders = await dbContext.CustomerOrder.AnyAsync(o => o.OrderStatusId == id);

        if (hasOrders)
        {
            return new DeleteOrderStatusResult { Success = false, Message = "این وضعیت در سفارشات استفاده شده و قابل حذف نیست" };
        }

        dbContext.OrderStatus.Remove(status);

        await dbContext.SaveChangesAsync();
        await _cache.RemoveAsync($"order_status_{status.Code}");

        return new DeleteOrderStatusResult { Success = true, Message = "وضعیت سفارش حذف شد" };
    }

    public async Task<BulkUpdateTrackingResult> BulkUpdateTracking(BulkUpdateTrackingCommand command)    
    {
        var result = new BulkUpdateTrackingResult();

        foreach (var entry in command.Entries)
        {
            var order = await dbContext.CustomerOrder.FirstOrDefaultAsync(o => o.OrderCode == entry.OrderCode);
            if (order is null)
            {
                result.NotFoundCodes.Add(entry.OrderCode);
                result.NotFoundCount++;
            }
            else
            {
                order.PostalTrackingCode = entry.PostalTrackingCode;
                order.UpdatedAt = DateTime.Now;
                result.UpdatedCount++;
            }
        }

        if (result.UpdatedCount > 0)
        {
            await dbContext.SaveChangesAsync();

            // Evict cache for every updated order so reads get fresh data
            foreach (var entry in command.Entries.Where(e => !result.NotFoundCodes.Contains(e.OrderCode)))
            {
                await _cache.RemoveAsync($"customer_order_{entry.OrderCode}");
                await _cache.RemoveAsync($"track_order_{entry.OrderCode}");
            }
        }

        result.Success = true;
        result.Message = $"{result.UpdatedCount} سفارش به‌روزرسانی شد" +
                         (result.NotFoundCount > 0 ? $"، {result.NotFoundCount} کد یافت نشد" : "");
        return result;
    }

    public async Task<TrackOrderResult> GetOrderByCode(string orderCode)
    {
        var trackCacheKey = $"track_order_{orderCode}";

        var cached = await _cache.GetOrDefaultAsync<TrackOrderResult?>(trackCacheKey);
        if (cached is not null)
            return cached;

        CustomerOrder? order;

        order = await dbContext.CustomerOrder
                               .AsNoTracking()
                               .Include(o => o.OrderStatus)
                               .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

        if (order is null)
        {
            order = await dbContext.CustomerOrder
                               .AsNoTracking()
                               .Include(o => o.OrderStatus)
                               .Include(o => o.Customer)
                               .OrderByDescending(o => o.CreatedAt)
                               .FirstOrDefaultAsync(o => o.Customer.PhoneNumber == orderCode.ToNormalizedPhoneNumber());
        }

        if (order is null)
        {
            return new() { Found = false };
        }

        var result = new TrackOrderResult
        {
            Found = true,
            OrderCode = order.OrderCode,
            StatusTitle = order.OrderStatus.Title,
            PostalTrackingCode = order.PostalTrackingCode,
            UpdatedAt = order.UpdatedAt
        };

        await _cache.SetAsync(trackCacheKey, result, new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromDays(1),
            DistributedCacheDuration = TimeSpan.FromDays(14)
        });

        return result;
    }

    public async Task<OrderStatisticsResult> GetOrderStatistics(GetOrderStatisticsQuery query)
    {
        OrderStatisticsResult result = new();

        var persianCalendar = new PersianCalendar();
        var calendarGenerator = new PersianCalendarGenerator();

        var currentYear = persianCalendar.GetYear(DateTime.Now);
        var currentMonth = persianCalendar.GetMonth(DateTime.Now);
        var currentYearCalendar = calendarGenerator.CreateYearCalendar(currentYear);

        var baseQuery = dbContext.CustomerOrder
            .Where(o => o.CreatedAt >= query.StartDate && o.CreatedAt <= query.EndDate);

        // Single DB query: aggregate to daily counts, never fetch full rows
        var dailyCounts = await baseQuery
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var countByDate = dailyCounts.ToDictionary(d => d.Date, d => d.Count);

        if (query.GroupBy == "week")
        {
            var currentMonthWeeks = currentYearCalendar.listMonths[currentMonth - 1];

            foreach (var week in currentMonthWeeks.ListWeeks)
            {
                if (week.ListDays.Count == 0) continue;

                var weekStart = week.ListDays.Min(d => d.GregorianDay.Date);
                var count = week.ListDays.Sum(d =>
                    countByDate.TryGetValue(d.GregorianDay.Date, out var c) ? c : 0);

                result.DataPoints.Add(new OrderStatisticsDataPoint
                {
                    Date = weekStart,
                    Label = $"هفته {week.WeekNumber +1}",
                    Count = count
                });
            }
        }
        else if (query.GroupBy == "month")
        {
            foreach (var month in currentYearCalendar.listMonths)
            {
                var days = month.ListWeeks.SelectMany(w => w.ListDays).ToList();
                
                if (days.Count == 0) 
                { 
                    continue; 
                }

                var monthStart = days.Min(d => d.GregorianDay.Date);

                var count = days.Sum(d =>
                    countByDate.TryGetValue(d.GregorianDay.Date, out var c) ? c : 0);

                result.DataPoints.Add(new OrderStatisticsDataPoint
                {
                    Date = monthStart,
                    Label = PersianCalendarTools.PersianMonthName(month.MonthNumber),
                    Count = count
                });
            }
        }
        else if (query.GroupBy == "year")
        {
            var yearGroups = dailyCounts
                .GroupBy(d => persianCalendar.GetYear(d.Date))
                .OrderBy(g => g.Key);

            foreach (var yearGroup in yearGroups)
            {
                result.DataPoints.Add(new OrderStatisticsDataPoint
                {
                    Date = yearGroup.Min(d => d.Date),
                    Label = yearGroup.Key.ToString(),
                    Count = yearGroup.Sum(d => d.Count)
                });
            }
        }

        return result;
    }
}
