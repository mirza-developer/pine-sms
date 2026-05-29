using Microsoft.EntityFrameworkCore;
using PineSms.Core.Contracts;
using PineSms.Core.Entities;
using PineSms.Core.Features.Order;
using PineSms.Persistence.Services;

namespace PineSms.Persistence.Repositories;

public class OrderRepository : IOrderService
{
    private readonly PineSmsDbContext dbContext;
    private readonly IBaleMessengerService baleMessengerService;

    public OrderRepository(PineSmsDbContext dbContext, IBaleMessengerService baleMessengerService)
    {
        this.dbContext = dbContext;
        this.baleMessengerService = baleMessengerService;
    }

    public async Task<NotifyOrderResult> NotifyOrder(NotifyOrderCommand command)
    {
        NotifyOrderResult result = new();

        var phone = NormalizePhoneNumber(command.CustomerPhoneNumber);

        if (phone.Length > 10)
        {
            result.Success = false;
            result.Message = $"شماره تلفن '{command.CustomerPhoneNumber}' معتبر نیست";
            return result;
        }

        var customer = await dbContext.Customer.FirstOrDefaultAsync(c => c.PhoneNumber == phone);
       
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
        var orderStatus = await dbContext.OrderStatus.FirstOrDefaultAsync(s => s.Code == command.OrderStatusCode);
      
        if (orderStatus is null)
        {
            result.Success = false;
           
            result.Message = $"وضعیت سفارش با کد '{command.OrderStatusCode}' یافت نشد";
            
            return result;
        }

        if (command.OrderCode.StartsWith("wc-"))
        {
            command.OrderCode = command.OrderCode.Replace("wc-","");
        }
 
        var order = await dbContext.CustomerOrder.FirstOrDefaultAsync(o => o.OrderCode == command.OrderCode);
       
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
            order.OrderStatusId = orderStatus.Id;

            order.UpdatedAt = DateTime.Now;
        }

        await dbContext.SaveChangesAsync();

        // 4. Notify the customer via Bale messenger
        //var notificationMessage = $"سفارش شما با کد {command.OrderCode} به وضعیت «{orderStatus.Title}» تغییر کرد.";
        //result.NotificationSent = await baleMessengerService.SendMessageAsync(phone, notificationMessage);

        result.Success = true;

        result.Message = result.IsNewOrder ? "سفارش ثبت شد" : "سفارش به‌روزرسانی شد";
       
        return result;
    }

    public async Task<List<OrderStatus>> GetAllOrderStatuses()
    {
        return await dbContext.OrderStatus.OrderBy(s => s.Code).ToListAsync();
    }

    public async Task<(bool success, string message)> UpsertOrderStatus(UpsertOrderStatusCommand command)
    {
        if (command.Id.HasValue)
        {
            var existing = await dbContext.OrderStatus.FindAsync(command.Id.Value);
            if (existing == null)
                return (false, "وضعیت سفارش یافت نشد");

            bool codeConflict = await dbContext.OrderStatus.AnyAsync(s => s.Code == command.Code && s.Id != command.Id.Value);
            if (codeConflict)
                return (false, "این کد قبلاً استفاده شده است");

            existing.Code = command.Code;
            existing.Title = command.Title;
            existing.LastChange = DateTime.Now;
        }
        else
        {
            bool codeConflict = await dbContext.OrderStatus.AnyAsync(s => s.Code == command.Code);
            if (codeConflict)
                return (false, "این کد قبلاً استفاده شده است");

            dbContext.OrderStatus.Add(new OrderStatus
            {
                Code = command.Code,
                Title = command.Title,
                LastChange = DateTime.Now
            });
        }

        await dbContext.SaveChangesAsync();
        return (true, command.Id.HasValue ? "وضعیت سفارش به‌روزرسانی شد" : "وضعیت سفارش ثبت شد");
    }

    public async Task<(bool success, string message)> DeleteOrderStatus(int id)
    {
        var status = await dbContext.OrderStatus.FindAsync(id);

        if (status is null)
        {
            return (false, "وضعیت سفارش یافت نشد");
        }

        bool hasOrders = await dbContext.CustomerOrder.AnyAsync(o => o.OrderStatusId == id);

        if (hasOrders)
        {
            return (false, "این وضعیت در سفارشات استفاده شده و قابل حذف نیست");
        }

        dbContext.OrderStatus.Remove(status);
        
        await dbContext.SaveChangesAsync();
        
        return (true, "وضعیت سفارش حذف شد");
    }

    public async Task<BulkUpdateTrackingResult> BulkUpdateTracking(BulkUpdateTrackingCommand command)    {
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
            await dbContext.SaveChangesAsync();

        result.Success = true;
        result.Message = $"{result.UpdatedCount} سفارش به‌روزرسانی شد" +
                         (result.NotFoundCount > 0 ? $"، {result.NotFoundCount} کد یافت نشد" : "");
        return result;
    }

    public async Task<TrackOrderResult> GetOrderByCode(string orderCode)
    {
        var order = await dbContext.CustomerOrder
            .Include(o => o.OrderStatus)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

        if (order is null)
        {
            return new()
            {
                Found = false
            };
        }

        return new()
        {
            Found = true,
            OrderCode = order.OrderCode,
            StatusTitle = order.OrderStatus.Title,
            PostalTrackingCode = order.PostalTrackingCode,
            UpdatedAt = order.UpdatedAt
        };
    }

    public async Task<OrderStatisticsResult> GetOrderStatistics(GetOrderStatisticsQuery query)
    {
        var orders = await dbContext.CustomerOrder
            .Where(o => o.CreatedAt >= query.StartDate && o.CreatedAt <= query.EndDate)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        var result = new OrderStatisticsResult();

        if (query.GroupBy == "day")
        {
            var grouped = orders.GroupBy(o => o.CreatedAt.Date)
                .Select(g => new OrderStatisticsDataPoint
                {
                    Date = g.Key,
                    Label = g.Key.ToString("yyyy/MM/dd"),
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();
            result.DataPoints = grouped;
        }
        else if (query.GroupBy == "week")
        {
            var grouped = orders.GroupBy(o => GetWeekOfYear(o.CreatedAt))
                .Select(g => new OrderStatisticsDataPoint
                {
                    Date = g.First().CreatedAt.Date,
                    Label = $"هفته {g.Key}",
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();
            result.DataPoints = grouped;
        }
        else if (query.GroupBy == "month")
        {
            var grouped = orders.GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new OrderStatisticsDataPoint
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Label = $"{g.Key.Year}/{g.Key.Month:D2}",
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();
            result.DataPoints = grouped;
        }
        else if (query.GroupBy == "year")
        {
            var grouped = orders.GroupBy(o => o.CreatedAt.Year)
                .Select(g => new OrderStatisticsDataPoint
                {
                    Date = new DateTime(g.Key, 1, 1),
                    Label = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();
            result.DataPoints = grouped;
        }

        return result;
    }

    private static int GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Saturday);
    }

    private static string NormalizePhoneNumber(string phone)
    {
        phone = phone.Trim();
        if (phone.StartsWith("+98"))
            phone = phone[3..];
        else if (phone.StartsWith("0098"))
            phone = phone[4..];
        else if (phone.StartsWith("98") && phone.Length == 12)
            phone = phone[2..];
        else if (phone.StartsWith("0"))
            phone = phone[1..];
        return phone;
    }
}
