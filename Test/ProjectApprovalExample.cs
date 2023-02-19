using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal class ProjectApprovalExample : ResumableFunctionLocal
{
    public Project CurrentProject { get; set; }
    public bool ManagerOneApproval { get; set; }
    public bool ManagerTwoApproval { get; set; }
    public bool ManagerThreeApproval { get; set; }

    //any method with attribute [ResumableFunctionEntryPoint] that takes no argument
    //and return IAsyncEnumerable<Wait> is a resumbale function
    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> SubFunctionTest()
    {
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

        AskManagerToApprove(CurrentProject.Id);
        Console.WriteLine("Wait sub function");
        yield return Wait("Wait sub function that waits two manager approval.", WaitTwoManagers);
        Console.WriteLine("After sub function ended");
        if (ManagerOneApproval && ManagerTwoApproval)
        {
            Console.WriteLine("Manager 1 & 2 approved the project");
            yield return
                Wait<ApprovalDecision, bool>("Manager Three Approve Project", ManagerThreeApproveProject)
                    .If((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerThreeApproval == output);

            if (ManagerThreeApproval)
                Console.WriteLine("Project Approved");
            else
                Console.WriteLine("Project Rejected");
        }
        else
        {
            Console.WriteLine("Project rejected by one of managers 1 & 2");
        }
        Success(nameof(SubFunctionTest));
    }

    [SubResumableFunction]
    public async IAsyncEnumerable<Wait> WaitTwoManagers()
    {
        Console.WriteLine("WaitTwoManagers started");
        yield return Wait(
            "Wait two methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output)
        ).All();
        Console.WriteLine("Two waits matched");
    }


    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> WaitFirst()
    {
        Console.WriteLine("First started");
        yield return Wait(
            "Wait first in two",
            new MethodWait<Project, bool>(ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input),
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output)
        ).First();
        Console.WriteLine("One of two waits matched");
    }

    [WaitMethod]
    internal bool PrivateMethod(Project project)
    {
        Console.WriteLine("Project Submitted");
        return true;
    }

    [WaitMethod]
    internal async Task<bool> ProjectSubmitted(Project project)
    {
        await Task.Delay(100);
        Console.WriteLine($"Project {project} Submitted ");
        return true;
    }

    [WaitMethod]
    public bool ManagerOneApproveProject(ApprovalDecision args)
    {
        Console.WriteLine($"Manager One Approve Project with decision ({args.Decision})");
        return true;
    }

    [WaitMethod]
    public bool ManagerTwoApproveProject(ApprovalDecision args)
    {
        Console.WriteLine($"Manager Two Approve Project with decision ({args.Decision})");
        return true;
    }

    [WaitMethod]
    public bool ManagerThreeApproveProject(ApprovalDecision args)
    {
        Console.WriteLine($"Manager Three Approve Project with decision ({args.Decision})");
        return true;
    }

    public bool AskManagerToApprove(int projectId)
    {
        Console.WriteLine("Ask Manager to Approve Project");
        return true;
    }

    public static Project GetCurrentProject()
    {
        return new Project { Id = 2000, Name = "Project Name", Description = "Description" };
    }
    protected void Success(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"^^^Success for [{msg}]^^^^");
        Console.ForegroundColor = ConsoleColor.White;
    }
}