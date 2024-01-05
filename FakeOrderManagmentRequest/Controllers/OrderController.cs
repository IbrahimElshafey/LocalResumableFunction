using FakeOrderManagmentRequest.Services;
using FakeOrderManagmentRequest.Workflow;
using Microsoft.AspNetCore.Mvc;
using ResumableFunctions.Handler.Attributes;

namespace FakeOrderManagmentRequest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        readonly IOrderProcessingService _orderService;
        public OrderController(IOrderProcessingService orderService)
        {
            this._orderService = orderService;
        }

        [HttpPost("OrderCreated")]
        public OrderCreationResult OrderCreated(Order order)
        {
            return _orderService.OrderCreated(order);
        }

        [HttpPost("FinanceTeamReviewTask")]
        public OrderReviewTaskResult FinanceTeamReviewTask(OrderReviewTask reviewTask)
        {
            return _orderService.FinanceTeamReviewTask(reviewTask);
        }

        [HttpPost("FinanceTeamRefundOrder")]
        public RefundTaskResult FinanceTeamRefundOrder(RefundTask refundTask)
        {
            return _orderService.FinanceTeamRefundOrder(refundTask);
        }
    }
}