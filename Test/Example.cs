using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LocalResumableFunction
{
    internal class Example : ResumableFunctionLocal
    {
        public Project CurrentProject { get; set; }
        public bool ManagerOneApproval { get; set; }
        public bool ManagerTwoApproval { get; set; }
        public bool ManagerThreeApproval { get; set; }

        //any method with attribute [ResumableFunctionEntryPoint] that takes no argument
        //and return IAsyncEnumerable<Wait> is a resumbale function
        [ResumableFunctionEntryPoint]
        public async IAsyncEnumerable<Wait> Start()
        {
            yield return
                When<Project, bool>("Project Sumbitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

            AskManagerToApprove(CurrentProject.Id);
            Console.WriteLine("Wait sub function");
            yield return WaitFunction("Wait sub function that waits two manager approval.", WaitTwoManagers);
            Console.WriteLine("After sub function ended");
            if (ManagerOneApproval == true && ManagerTwoApproval == true)
            {
                Console.WriteLine("Manager 1 & 2 approved the project");
                yield return
                    When<ApprovalDecision, bool>("Manager Three Approve Project", ManagerThreeApproveProject)
                    .If((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerThreeApproval == output);

                if (ManagerThreeApproval)
                    Console.WriteLine("Project Approved");
                else
                    Console.WriteLine("Project Rejected");
            }
            else
                Console.WriteLine("Project rejected by one of managers 1 & 2");
        }

        [SubResumableFunction]
        public async IAsyncEnumerable<Wait> WaitTwoManagers()
        {
            Console.WriteLine("WaitTwoManagers started");
            yield return When(
                "Wait two methods",
                new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                    .If((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerOneApproval == output),
                new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                    .If((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerTwoApproval == output)
                ).WaitAll();
            Console.WriteLine("Two waits matched");
        }



        [ResumableFunctionEntryPoint]
        public async IAsyncEnumerable<Wait> WaitFirst()
        {
            Console.WriteLine("WaitFirst started");
            yield return When(
                "Wait first in two",
                new MethodWait<Project, bool>(ProjectSubmitted)
                    .If((input, output) => output == true)
                    .SetData((input, output) => CurrentProject == input),
                new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                    .If((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerOneApproval == output)
            ).WaitFirst();
            Console.WriteLine("One of two waits matched");
        }

        [WaitMethod]
        public bool ProjectSubmitted(Project project)
        {
            Console.WriteLine("Project Submitted");
            return true;
        }

        [WaitMethod]
        public bool ManagerOneApproveProject(ApprovalDecision args)
        {
            Console.WriteLine("Manager One Approve Project");
            return true;
        }

        [WaitMethod]
        public bool ManagerTwoApproveProject(ApprovalDecision args)
        {
            Console.WriteLine("Manager Two Approve Project");
            return true;
        }
        
        [WaitMethod]
        public bool ManagerThreeApproveProject(ApprovalDecision args)
        {
            Console.WriteLine("Manager Three Approve Project");
            return true;
        }

        public bool AskManagerToApprove(int projectId)
        {
            Console.WriteLine("Ask Manager to Approve Project");
            return true;
        }
    }

    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public record ApprovalDecision(int ProjectId, bool Decision);
}
