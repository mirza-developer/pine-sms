using PineSms.Core.Entities;
using PineSms.Core.Features.Order;

namespace PineSms.Core.Contracts;

public interface IOrderService
{
    Task<NotifyOrderResult> NotifyOrder(NotifyOrderCommand command);
    Task<List<OrderStatus>> GetAllOrderStatuses();
    Task<UpsertOrderStatusResult> UpsertOrderStatus(UpsertOrderStatusCommand command);
    Task<DeleteOrderStatusResult> DeleteOrderStatus(int id);
    Task<BulkUpdateTrackingResult> BulkUpdateTracking(BulkUpdateTrackingCommand command);
    Task<TrackOrderResult> GetOrderByCode(string orderCode);
    Task<OrderStatisticsResult> GetOrderStatistics(GetOrderStatisticsQuery query);
}
