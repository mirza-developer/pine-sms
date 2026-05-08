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

        // 3. Upsert customer order
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
        if (status == null)
            return (false, "وضعیت سفارش یافت نشد");

        bool hasOrders = await dbContext.CustomerOrder.AnyAsync(o => o.OrderStatusId == id);
        if (hasOrders)
            return (false, "این وضعیت در سفارشات استفاده شده و قابل حذف نیست");

        dbContext.OrderStatus.Remove(status);
        await dbContext.SaveChangesAsync();
        return (true, "وضعیت سفارش حذف شد");
    }

    private static string NormalizePhoneNumber(string phone)
    {
        phone = phone.Trim();
        if (phone.StartsWith("+98"))
            phone = phone[3..];
        else if (phone.StartsWith("0098"))
            phone = phone[4..];
        else if (phone.StartsWith("0"))
            phone = phone[1..];
        return phone;
    }
}
