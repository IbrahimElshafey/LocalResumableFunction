using Microsoft.AspNetCore.Mvc;
using TestApi1.Examples;

namespace TestApi1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        readonly ProjectApprovalExample example = new ProjectApprovalExample();

        [HttpPost(nameof(ProjectSubmitted))]
        public async Task<bool> ProjectSubmitted(Project project)
        {
            return await example.ProjectSubmitted(project);
        }

        [HttpPost(nameof(ManagerOneApproveProject))]
        public bool ManagerOneApproveProject(ApprovalDecision args)
        {
            return example.ManagerOneApproveProject(args);
        }

        [HttpPost(nameof(ManagerTwoApproveProject))]
        public bool ManagerTwoApproveProject(ApprovalDecision args)
        {
            return example.ManagerTwoApproveProject(args);
        }

        [HttpPost(nameof(ManagerThreeApproveProject))]
        public bool ManagerThreeApproveProject(ApprovalDecision args)
        {
            return example.ManagerThreeApproveProject(args);
        }

        [HttpPost(nameof(ManagerFourApproveProject))]
        public bool ManagerFourApproveProject(ApprovalDecision args)
        {
            return example.ManagerFourApproveProject(args);
        }

        [HttpPost(nameof(ManagerFiveApproveProject))]
        public bool ManagerFiveApproveProject(ApprovalDecision args)
        {
            return example.ManagerFiveApproveProject(args);
        }
    }
}