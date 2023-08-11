using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.BaseUse;

namespace TestApi1.Examples;

public class ProjectApprovalExample : ResumableFunctionsContainer, IManagerFiveApproval
{
    public Project CurrentProject { get; set; }
    public bool ManagerOneApproval { get; set; }
    public bool ManagerTwoApproval { get; set; }
    public bool ManagerThreeApproval { get; set; }
    public bool ManagerFourApproval { get; set; }
    public bool ManagerFiveApproval { get; set; }
    public string ExternalMethodStatus { get; set; } = "Not matched yet.";

    [ResumableFunctionEntryPoint("ProjectApprovalExample.ProjectApprovalFlow", isActive: true)]//Point 1
    public async IAsyncEnumerable<Wait> ProjectApprovalFlow()
    {
        //throw new NotImplementedException("Exception on get first wait.");
        yield return
         Wait<Project, bool>(ProjectSubmitted, "Project Submitted")//Point 2
             .MatchIf((project, output) => output && !project.IsResubmit)//Point 3
             .AfterMatch((project, output) => CurrentProject = project);//Point 4
        Log("###After Project Submitted");
        //throw new NotImplementedException("Exception after first wait match.");
        await AskManagerToApprove("Manager One", CurrentProject.Id);
        //throw new Exception("Critical exception aftrer AskManagerToApprove");
        yield return
               Wait<ApprovalDecision, bool>(ManagerOneApproveProject, "Manager One Approve Project")
                   .MatchIf((approvalDecision, output) => approvalDecision.ProjectId == CurrentProject.Id)
                   .AfterMatch((approvalDecision, approvalResult) => ManagerOneApproval = approvalResult);

        if (ManagerOneApproval is false)
        {
            Log("Go back and ask applicant to resubmitt project.");
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

    [ResumableFunctionEntryPoint("ProjectApprovalExample.ExternalMethod")]
    public async IAsyncEnumerable<Wait> ExternalMethod()
    {
        await Task.Delay(1);
        yield return Wait<string, string>
                (new ExternalServiceClass().SayHelloExport, "Wait say hello external")
                .MatchIf((userName, helloMsg) => userName.StartsWith("M"))
                .AfterMatch((userName, helloMsg) => ExternalMethodStatus = $"Say hello called and user name is: {userName}");

        yield return
              Wait<object, int>(new ExternalServiceClass().ExternalMethodTest, "Wait external method 1")
                  .MatchIf((input, output) => output % 2 == 0)
                  .AfterMatch((input, output) => ExternalMethodStatus = "ExternalMethodTest Matched.");

        yield return
          Wait<string, int>(new ExternalServiceClass().ExternalMethodTest2, "Wait external method 2")
              .MatchIf((input, output) => input == "Ibrahim")
              .AfterMatch((input, output) => ExternalMethodStatus = "ExternalMethodTest2 Matched.");

        Success(nameof(ExternalMethod));
    }

    [ResumableFunctionEntryPoint("ProjectApprovalExample.ExternalMethodWaitGoodby")]
    public async IAsyncEnumerable<Wait> ExternalMethodWaitGoodby()
    {
        await Task.Delay(1);
        yield return Wait<string, string>
                (new ExternalServiceClass().SayGoodby, "Wait good by external")
                .MatchIf((userName, helloMsg) => userName[0] == 'M')
                .AfterMatch((userName, helloMsg) => ExternalMethodStatus = $"Say goodby called and user name is: {userName}");
        Success(nameof(ExternalMethodWaitGoodby));
    }
    //any method with attribute [ResumableFunctionEntryPoint] that takes no argument
    //and return IAsyncEnumerable<WaitX> is a resumbale function
    [ResumableFunctionEntryPoint("PAE.InterfaceMethod")]
    public async IAsyncEnumerable<Wait> InterfaceMethod()
    {
        yield return
         Wait<Project, bool>(ProjectSubmitted, "Project Submitted")
             .MatchIf((input, output) => output == true)
             .AfterMatch((input, output) => CurrentProject = input);

        yield return
               Wait<ApprovalDecision, bool>(FiveApproveProject, "Manager Five Approve Project")
                   .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                   .AfterMatch((input, output) => ManagerFiveApproval = output);
        Success(nameof(InterfaceMethod));
    }
    public async IAsyncEnumerable<Wait> SubFunctionTest()
    {
        yield return
            Wait<Project, bool>(ProjectSubmitted, "Project Submitted")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input);

        await AskManagerToApprove("Manager 1", CurrentProject.Id);
        WriteMessage("Wait sub function");
        yield return Wait("Wait sub function that waits two manager approval.", WaitTwoManagers);
        WriteMessage("After sub function ended");
        if (ManagerOneApproval && ManagerTwoApproval)
        {
            WriteMessage("Manager 1 & 2 approved the project");
            yield return
                Wait<ApprovalDecision, bool>(ManagerThreeApproveProject, "Manager Three Approve Project")
                    .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                    .AfterMatch((input, output) => ManagerThreeApproval = output);

            WriteMessage(ManagerThreeApproval ? "Project Approved" : "Project Rejected");
        }
        else
        {
            WriteMessage("Project rejected by one of managers 1 & 2");
        }
        Success(nameof(SubFunctionTest));
    }

    [SubResumableFunction("ProjectApprovalExample.WaitTwoManagers")]
    public async IAsyncEnumerable<Wait> WaitTwoManagers()
    {
        WriteMessage("WaitTwoManagers started");
        yield return Wait(
            "Wait two methods",
            Wait<ApprovalDecision, bool>(ManagerOneApproveProject, "Manager One Approve Project")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerOneApproval = output),
            Wait<ApprovalDecision, bool>(ManagerTwoApproveProject, "Manager Two Approve Project")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerTwoApproval = output)
        ).MatchAll();
        WriteMessage("Two waits matched");
    }


    //[ResumableFunctionEntryPoint]
    public async IAsyncEnumerable<Wait> WaitFirst()
    {
        WriteMessage("First started");
        yield return Wait(
            "Wait first in two",
            Wait<Project, bool>(ProjectSubmitted, "Project Submitted")
                .MatchIf((input, output) => output == true)
                .AfterMatch((input, output) => CurrentProject = input),
            Wait<ApprovalDecision, bool>(ManagerOneApproveProject, "Manager One Approve Project")
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .AfterMatch((input, output) => ManagerOneApproval = output)
        ).MatchAny();
        WriteMessage("One of two waits matched");
    }

    [PushCall("ProjectApprovalExample.PrivateMethod")]
    internal bool PrivateMethod(Project project)
    {
        WriteMessage("Project Submitted");
        return true;
    }

    [PushCall("ProjectApprovalExample.ProjectSubmitted")]
    internal async Task<bool> ProjectSubmitted(Project project)
    {
        //await Task.Delay(100);
        WriteAction($"Project {project} Submitted ");
        return true;
    }

    [PushCall("ProjectApprovalExample.ManagerOneApproveProject")]
    public bool ManagerOneApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager One Approve Project with decision ({args.Decision})");
        return args.Decision;
    }

    [PushCall("ProjectApprovalExample.ManagerTwoApproveProject")]
    public bool ManagerTwoApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager Two Approve Project with decision ({args.Decision})");
        return args.Decision;
    }

    [PushCall("ProjectApprovalExample.ManagerThreeApproveProject")]
    public bool ManagerThreeApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager Three Approve Project with decision ({args.Decision})");
        return args.Decision;
    }


    [PushCall("ProjectApprovalExample.ManagerFourApproveProject")]
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
        Console.ResetColor();
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

    [PushCall("IManagerFiveApproval.ManagerFiveApproveProject", CanPublishFromExternal = true)]
    public bool FiveApproveProject(ApprovalDecision args)
    {
        WriteAction($"Manager Four Approve Project with decision ({args.Decision})");
        return args.Decision;
    }
}

