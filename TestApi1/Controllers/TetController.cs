using Microsoft.AspNetCore.Mvc;
using TestApi1.Examples;

namespace TestApi1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TetController : ControllerBase
    {
        readonly ProjectApprovalExample example = new ProjectApprovalExample();

        [HttpPost(nameof(ManagerOneApproveProject))]
        public bool ManagerOneApproveProject(ApprovalDecision args)
        {
            return example.ManagerOneApproveProject(args);
        }

        [HttpPost(nameof(ProjectSubmitted))]
        public async Task<bool> ProjectSubmitted(Project project)
        {
            return await example.ProjectSubmitted(project);
        }
    }
}