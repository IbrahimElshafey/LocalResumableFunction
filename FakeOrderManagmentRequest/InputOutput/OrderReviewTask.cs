namespace FakeOrderManagmentRequest.Workflow
{
    public class OrderReviewTask
    {
        public int TaskId { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public OrderReviewTask()
        {
        }
    }
}
