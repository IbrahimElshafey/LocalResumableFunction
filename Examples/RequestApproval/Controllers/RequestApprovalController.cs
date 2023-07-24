using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.InOuts;

namespace RequestApproval.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RequestApprovalController : ControllerBase
    {
        private readonly ILogger<RequestApprovalController> _logger;
        private readonly IRequestApprovalService service;

        public RequestApprovalController(ILogger<RequestApprovalController> logger, IRequestApprovalService service)
        {
            _logger = logger;
            this.service = service;
        }

        [HttpPost(nameof(UserSubmitRequest))]
        public bool UserSubmitRequest(Request request)
        {
            return service.UserSubmitRequest(request);
        }

        [HttpPost(nameof(ManagerApproval))]
        public int ManagerApproval(ApproveRequestArgs input)
        {
            return service.ManagerApproval(input);
        }

    }
    public class RequestApprovalWorkflow : ResumableFunctionsContainer
    {
        private const string WaitSubmitRequest = "Wait User Submit Request";
        private IRequestApprovalService _service;

        public void SetDependencies(IRequestApprovalService service)
        {
            _service = service;
        }

        public Request UserRequest { get; set; }
        public int ManagerApprovalTaskId { get; set; }
        public ApproveRequestArgs ManagerApprovalResult { get; set; }

        [ResumableFunctionEntryPoint("RequestApprovalWorkflow.RequestApprovalFlow")]
        internal async IAsyncEnumerable<Wait> RequestApprovalFlow()
        {
            yield return WaitUserSubmitRequest();

            if (ManagerApprovalTaskId != default)
                Console.WriteLine($"Request `{UserRequest.Id}` re-submitted.");

            ManagerApprovalTaskId = _service.AskManagerApproval(UserRequest.Id);
            //throw new Exception("Exception after first wait");
            yield return WaitManagerApproval();

            switch (ManagerApprovalResult.Decision)
            {
                case "Accept":
                    _service.InformUserAboutAccept(UserRequest.Id);
                    break;
                case "Reject":
                    _service.InformUserAboutReject(UserRequest.Id);
                    break;
                case "MoreInfo":
                    _service.AskUserForMoreInfo(UserRequest.Id, ManagerApprovalResult.Message);
                    yield return GoBackTo<Request, bool>(WaitSubmitRequest, (request, result) => request.Id == UserRequest.Id);
                    break;
                default: throw new ArgumentException("Allowed values for decision are one of(Accept,Reject,MoreInfo)");
            }

            await Task.Delay(100);
            Console.WriteLine("RequestApprovalFlow Ended");
        }

        private Wait WaitUserSubmitRequest()
        {
            return Wait<Request, bool>(_service.UserSubmitRequest, WaitSubmitRequest)
                    .MatchIf((request, result) => request.Id > 0)
                    .SetData((request, result) => UserRequest == request);
        }

        private Wait WaitManagerApproval()
        {
            return Wait<ApproveRequestArgs, int>(_service.ManagerApproval, "Wait Manager Approval")
                    .MatchIf((approveRequestArgs, approvalId) => approvalId > 0 && approveRequestArgs.TaskId == ManagerApprovalTaskId)
                    .SetData((approveRequestArgs, approvalId) => ManagerApprovalResult == approveRequestArgs);
        }

        public override Task OnErrorOccurred(string message, Exception ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.WriteLine(ex?.Message);
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
    public class RequestApprovalService : IRequestApprovalService
    {
        [PushCall("RequestApproval.UserSubmitRequest")]
        public bool UserSubmitRequest(Request request)
        {
            Console.WriteLine($"Request `{request.Id}` submitted.");
            return true;
        }

        public int AskManagerApproval(int requestId)
        {
            Console.WriteLine($"Ask manager to approve request `{requestId}`");
            return requestId + 10;//taskId
        }

        [PushCall("RequestApproval.ManagerApproval")]
        public int ManagerApproval(ApproveRequestArgs input)
        {
            Console.WriteLine($"Manager approval for task `{input.TaskId}`");
            return Random.Shared.Next();//approval id
        }

        public void InformUserAboutAccept(int id)
        {
            //some code
            Console.WriteLine($"Inform user about accept request `{id}`");
        }

        public void InformUserAboutReject(int id)
        {
            //some code
            Console.WriteLine($"Inform user about reject request `{id}`");
        }

        public void AskUserForMoreInfo(int id, string message)
        {
            //some code
            Console.WriteLine($"Ask user for more info about request `{id}`");
        }
    }
    public class Request
    {
        public int Id { get; set; }
        public string SomeContent { get; set; }
    }

    public class ApproveRequestArgs
    {
        public int TaskId { get; set; }
        public string Decision { get; set; } = "Accept";
        public string Message { get; set; }
    }
}