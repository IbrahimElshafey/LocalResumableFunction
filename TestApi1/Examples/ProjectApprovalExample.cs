using ResumableFunctions.Core;
using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;

namespace TestApi1.Examples;

internal class ProjectApprovalExample : ResumableFunction, IManagerFiveApproval
{
    public Project CurrentProject { get; set; }
    public bool ManagerOneApproval { get; set; }
    public bool ManagerTwoApproval { get; set; }
    public bool ManagerThreeApproval { get; set; }
    public bool ManagerFourApproval { get; set; }
    public bool ManagerFiveApproval { get; set; }
    public string ExternalMethodStatus { get; private set; } = "Not matched yet.";

    [ResumableFunctionEntryPoint]//Point 1
    public async IAsyncEnumerable<Wait> ProjectApprovalFlow()
    {
        yield return
         Wait<Project, bool>("Project Submitted", ProjectSubmitted)//Point 2
             .MatchIf((project, output) => output && !project.IsResubmit)//Point 3
             .SetData((project, output) => CurrentProject == project);//Point 4

        await AskManagerToApprove("Manager One", CurrentProject.Id);
        yield return
               Wait<ApprovalDecision, bool>("Manager One Approve Project", ManagerOneApproveProject)
                   .MatchIf((approvalDecision, output) => approvalDecision.ProjectId == CurrentProject.Id)
                   .SetData((approvalDecision, approvalResult) => ManagerOneApproval == approvalResult);

        if (ManagerOneApproval is false)
        {
            WriteMessage("Go back and ask applicant to resubmitt project.");
            await AskApplicantToResubmittProject(CurrentProject.Id);
            yield return GoBackTo<Project, bool>("Project Submitted", (project, output) => output && project.IsResubmit && project.Id == CurrentProject.Id);
        }
        else
        {
            WriteMessage("Project approved");
            await InfromApplicantAboutApproval(CurrentProject.Id);
        }
        Success(nameof(ProjectApprovalFlow));
    }

    private Task InfromApplicantAboutApproval(int id)
    {
        return Task.CompletedTask;
    }

    private Task AskApplicantToResubmittProject(int id)
    {
        return Task.CompletedTask;
    }

    [ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> ExternalMethod()
    {
        await Task.Delay(1);

        yield return
              Wait<object, int>(
                  "Wait external method 1",
              new ExternalServiceClass().ExternalMethodTest)
                  .MatchIf((input, output) => output % 2 == 0)
                  .SetData((input, output) => ExternalMethodStatus == "ExternalMethodTest Matched.");

        yield return
          Wait<string, int>(
              "Wait external method 2",
          new ExternalServiceClass().ExternalMethodTest2)
              .MatchIf((input, output) => input == "Ibrahim")
              .SetData((input, output) => ExternalMethodStatus == "ExternalMethodTest2 Matched.");

        Success(nameof(ExternalMethod));
    }
    //any method with attribute [ResumableFunctionEntryPoint] that takes no argument
    //and return IAsyncEnumerable<Wait> is a resumbale function
    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> InterfaceMethod()
    {
        yield return
         Wait<Project, bool>("Project Submitted", ProjectSubmitted)
             .MatchIf((input, output) => output == true)
             .SetData((input, output) => CurrentProject == input);

        yield return
               Wait<ApprovalDecision, bool>("Manager Five Approve Project", ManagerFiveApproveProject)
                   .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                   .SetData((input, output) => ManagerFiveApproval == output);
        Success(nameof(InterfaceMethod));
    }
    public async IAsyncEnumerable<Wait> SubFunctionTest()
    {
        yield return
            Wait<Project, bool>("Project Submitted", ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input);

        await AskManagerToApprove("Manager 1", CurrentProject.Id);
        WriteMessage("Wait sub function");
        yield return Wait("Wait sub function that waits two manager approval.", WaitTwoManagers);
        WriteMessage("After sub function ended");
        if (ManagerOneApproval && ManagerTwoApproval)
        {
            WriteMessage("Manager 1 & 2 approved the project");
            yield return
                Wait<ApprovalDecision, bool>("Manager Three Approve Project", ManagerThreeApproveProject)
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
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
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
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
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input),
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
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
        //await Task.Delay(100);
        WriteAction($"Project {project} Submitted ");
        return true;
    }

    [WaitMethod]
    public bool ManagerOneApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager One Approve Project with decision ({args.Decision})");
        return args.Decision;
    }

    [WaitMethod]
    public bool ManagerTwoApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager Two Approve Project with decision ({args.Decision})");
        return args.Decision;
    }

    [WaitMethod]
    public bool ManagerThreeApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager Three Approve Project with decision ({args.Decision})");
        return args.Decision;
    }


    [WaitMethod]
    public bool ManagerFourApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager Four Approve Project with decision ({args.Decision})");
        return args.Decision;
    }

    public async Task<bool> AskManagerToApprove(string manager, int projectId)
    {
        await Task.Delay(10);
        WriteAction($"Ask Manager [{manager}] to Approve Project that has id [{projectId}]");
        return true;
    }

    public static Project GetCurrentProject()
    {
        return new Project { Id = Random.Shared.Next(1, int.MaxValue), Name = "Project Name", Description = "Description" };
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

    [WaitMethodImplementation]
    public bool ManagerFiveApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager Four Approve Project with decision ({args.Decision})");
        return args.Decision;
    }
}

