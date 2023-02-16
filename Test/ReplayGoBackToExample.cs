using LocalResumableFunction;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

namespace Test
{
    internal class ReplayGoBackToExample : Example
    {
        private const string ProjectSumbitted = "Project Sumbitted";

        [ResumableFunctionEntryPoint]
        public async IAsyncEnumerable<Wait> TestReplay()
        {
            yield return
                When<Project, bool>(ProjectSumbitted, ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

            AskManagerToApprove(CurrentProject.Id);
            yield return When<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
                    .If((input, output) => output == true)
                    .SetData((input, output) => ManagerOneApproval == input.Decision);

            if (ManagerOneApproval is false)
            {
                Console.WriteLine("ReplayExample: Manager one rejected project and repaly will go to ProjectSumbitted.");
                yield return
                    GoBackTo<Project, bool>(
                        ProjectSumbitted,
                        (input, output) => input.Id == CurrentProject.Id);
            }
            else
                Console.WriteLine("ReplayExample: Manager one approved project");
        }
    }
}
