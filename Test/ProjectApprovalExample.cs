using LocalResumableFunction;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;

namespace Test;

internal class ProjectApprovalExample : ResumableFunctionLocal
{
    public Project CurrentProject { get; set; }
    public bool ManagerOneApproval { get; set; }
    public bool ManagerTwoApproval { get; set; }
    public bool ManagerThreeApproval { get; set; }
    public bool ManagerFourApproval { get; set; }

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
        WriteMessage("Wait sub function");
        yield return Wait("Wait sub function that waits two manager approval.", WaitTwoManagers);
        WriteMessage("After sub function ended");
        if (ManagerOneApproval && ManagerTwoApproval)
        {
            WriteMessage("Manager 1 & 2 approved the project");
            yield return
                Wait<ApprovalDecision, bool>("Manager Three Approve Project", ManagerThreeApproveProject)
                    .If((input, output) => input.ProjectId == CurrentProject.Id)
                    .SetData((input, output) => ManagerThreeApproval == output);

            WriteMessage(ManagerThreeApproval ? "Project Approved" : "Project Rejected");
        }
        else
        {
            WriteMessage("Project rejected by one of managers 1 & 2");
        }
        Success(nameof(SubFunctionTest));
    }

    [SubResumableFunction]
    public async IAsyncEnumerable<Wait> WaitTwoManagers()
    {
        WriteMessage("WaitTwoManagers started");
        yield return Wait(
            "Wait two methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output)
        ).All();
        WriteMessage("Two waits matched");
    }


    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> WaitFirst()
    {
        WriteMessage("First started");
        yield return Wait(
            "Wait first in two",
            new MethodWait<Project, bool>(ProjectSubmitted)
                .If((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input),
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .If((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output)
        ).First();
        WriteMessage("One of two waits matched");
    }

    [WaitMethod]
    internal bool PrivateMethod(Project project)
    {
        WriteMessage("Project Submitted");
        return true;
    }

    [WaitMethod]
    internal async Task<bool> ProjectSubmitted(Project project)
    {
        await Task.Delay(100);
        WriteAction($"Project {project} Submitted ");
        return true;
    }

    [WaitMethod]
    public bool ManagerOneApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager One Approve Project with decision ({args.Decision})");
        return true;
    }

    [WaitMethod]
    public bool ManagerTwoApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager Two Approve Project with decision ({args.Decision})");
        return true;
    }

    [WaitMethod]
    public bool ManagerThreeApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager Three Approve Project with decision ({args.Decision})");
        return true;
    }


    [WaitMethod]
    public bool ManagerFourApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager Four Approve Project with decision ({args.Decision})");
        return true;
    }

    public bool AskManagerToApprove(int projectId)
    {
        WriteAction("Ask Manager to Approve Project");
        return true;
    }

    public static Project GetCurrentProject()
    {
        return new Project { Id = Random.Shared.Next(1,int.MaxValue), Name = "Project Name", Description = "Description" };
    }
    protected void Success(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"^^^Success for [{msg}]^^^^");
        Console.ForegroundColor = ConsoleColor.White;
    }
    protected void WriteMessage(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{msg} -{CurrentProject?.Id}");
        Console.ForegroundColor = ConsoleColor.White;
    }

    protected void WriteAction(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{msg} -{CurrentProject?.Id}");
        Console.ForegroundColor = ConsoleColor.White;
    }
}