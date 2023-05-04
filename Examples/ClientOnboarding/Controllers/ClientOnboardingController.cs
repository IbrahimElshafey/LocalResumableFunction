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
        private readonly IClientOnboardingService service;

        public ClientOnboardingController(
            ILogger<ClientOnboardingController> logger,
            IClientOnboardingService service)
        {
            _logger = logger;
            this.service = service;
            
        }

        [HttpPost(nameof(ClientFillsForm))]
        public RegistrationResult ClientFillsForm(RegistrationForm registrationForm)
        {
            return service.ClientFillsForm(registrationForm);
        }

        [HttpPost(nameof(OwnerApproveClient))]
        public OwnerApproveClientResult OwnerApproveClient(OwnerApproveClientInput ownerApproveClientInput)
        {
            return service.OwnerApproveClient(ownerApproveClientInput);
        }

        [HttpGet(nameof(SendMeetingResult))]
        public MeetingResult SendMeetingResult(int meetingId)
        {
            return service.SendMeetingResult(meetingId);
        }
    }
}