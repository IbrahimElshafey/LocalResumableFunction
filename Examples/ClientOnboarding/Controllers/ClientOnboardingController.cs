using ClientOnboarding.InOuts;
using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using Microsoft.AspNetCore.Mvc;
using ResumableFunctions.Handler.Helpers;
using System.Threading.Tasks;

namespace ClientOnboarding.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClientOnboardingController : ControllerBase
    {


        private readonly ILogger<ClientOnboardingController> _logger;
        private readonly ClientOnboardingService service;

        public ClientOnboardingController(
            ILogger<ClientOnboardingController> logger,
            ClientOnboardingService service)
        {
            _logger = logger;
            this.service = service;
            
        }

        [HttpPost(nameof(ClientFillsForm))]
        public RegistrationResult ClientFillsForm(RegistrationForm registrationForm)
        {
            return service.ClientFillsForm(registrationForm);
        }

        [HttpGet(nameof(OwnerApproveClient))]
        public OwnerApproveClientResult OwnerApproveClient(int taskId)
        {
            return service.OwnerApproveClient(taskId);
        }

        [HttpGet(nameof(SendMeetingResult))]
        public MeetingResult SendMeetingResult(int meetingId)
        {
            return service.SendMeetingResult(meetingId);
        }
    }
}