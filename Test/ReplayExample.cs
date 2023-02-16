using LocalResumableFunction;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    internal class ReplayExample : Example
    {
        [ResumableFunctionEntryPoint]
        public async IAsyncEnumerable<Wait> TestReplay()
        {
            yield return
                When<Project, bool>("Project Sumbitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

            AskManagerToApprove(CurrentProject.Id);
            yield return When<ApprovalDecision, bool>("", ManagerOneApproveProject)
                    .If((input, output) => output == true)
                    .SetData((input, output) => ManagerOneApproval == input.Decision);
        }
    }
}
