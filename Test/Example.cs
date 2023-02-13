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
        public bool ManagerApproval { get; set; }


        //any method with attribute [ResumableFunctionEntryPoint] that takes no argument
        //and return IAsyncEnumerable<Wait> is a resumbale function
        [ResumableFunctionEntryPoint]
        public async IAsyncEnumerable<Wait> Start()
        {
            yield return
                When<Project, bool>("Project Sumbitted",ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject.SetValue(input));

            AskManagerToApprove(CurrentProject.Id);

            yield return
                When<(int ProjectId,bool Decision), bool>("Manager Approve Project",ManagerApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerApproval.SetValue(output));

            if (ManagerApproval)
                Console.WriteLine("Project Approved");
            else
                Console.WriteLine("Project Rejected");
        }

        [WaitMethod]
        public bool ProjectSubmitted(Project project)
        {
            return true;
        }

        [WaitMethod]
        public bool ManagerApproveProject((int projectId,bool decision)args)
        {
            Console.WriteLine("Manager Approve Project");
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
}
