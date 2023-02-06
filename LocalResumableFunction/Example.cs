using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalResumableFunction
{
    internal class Example : ResumableFunctionLocal
    {
        public Project CurrentProject { get; set; }
        public bool ManagerApproval { get; set; }

        [ResumableMethodStart]
        public async IAsyncEnumerable<Wait> Start()
        {
            yield return
                When<Project, bool>(ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject.SetValue(input));

            AskManagerToApprove(CurrentProject.Id);

            yield return
                When<int, bool>(ManagerApproveProject)
                .If((input, output) => input == CurrentProject.Id)
                .SetData((input, output) => ManagerApproval.SetValue(output));

            if (ManagerApproval)
                Console.WriteLine("Project Approved");
            else
                Console.WriteLine("Project Rejected");
        }

        [EventMethod]
        public bool ProjectSubmitted(Project project)
        {
            return true;
        }

        public bool ManagerApproveProject(int projectId)
        {
            return true;
        }

        public bool AskManagerToApprove(int projectId)
        {
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
