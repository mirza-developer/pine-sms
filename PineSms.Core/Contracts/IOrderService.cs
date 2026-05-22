using PineSms.Core.Entities;
using PineSms.Core.Features.Order;

namespace PineSms.Core.Contracts;

public interface IOrderService
{
    Task<NotifyOrderResult> NotifyOrder(NotifyOrderCommand command);
    Task<List<OrderStatus>> GetAllOrderStatuses();
    Task<(bool success, string message)> UpsertOrderStatus(UpsertOrderStatusCommand command);
    Task<(bool success, string message)> DeleteOrderStatus(int id);
    Task<BulkUpdateTrackingResult> BulkUpdateTracking(BulkUpdateTrackingCommand command);
    Task<TrackOrderResult> GetOrderByCode(string orderCode);
}
