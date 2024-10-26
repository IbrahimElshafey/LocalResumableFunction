using FakeOrderManagmentRequest.Workflow;

namespace FakeOrderManagmentRequest.Services
{
    public interface IOrderProcessingService
    {
        int CreateRefundTask(int orderId);
        RefundTaskResult FinanceTeamRefundOrder(RefundTask refundTask);
        OrderReviewTaskResult FinanceTeamReviewTask(OrderReviewTask reviewTask);
        OrderCreationResult OrderCreated(Order order);
        Task<int> ReviewOrderTask(int orderId);
        void SendNotification(int clientId, string msg);
    }
}
