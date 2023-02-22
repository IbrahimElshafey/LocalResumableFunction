// See https://aka.ms/new-console-template for more information

using LocalResumableFunction;
using LocalResumableFunction.Data;
using LocalResumableFunction.InOuts;
using System.Reflection;
using Test;

public class Program
{
    private static Scanner _scanner;

    private static async Task Main()
    {
        _scanner = new Scanner
        {
            _context = new FunctionDataContext()
        };
        Console.WriteLine("Test App RUNNING.");

        //await TestSubFunctionCall();
        await TestReplayGoBackAfter();
        await TestReplayGoBackBeforeNewMatch();

        //await TestWaitMany();
        //await TestWaitManyFunctions();
        //await TestLoops();

        //await TestParallelScenarios();
        Console.ReadLine();
    }

    

    private static async Task TestParallelScenarios()
    {
        await Task.WhenAll(
            TestSubFunctionCall(),
            TestReplayGoBackAfter(),
            TestReplayGoBackBeforeNewMatch(),
            TestWaitMany(),
            TestWaitManyFunctions());
        //await TestSubFunctionCall();
        //await TestReplayGoBackAfter();
        //await TestReplayGoBackBeforeNewMatch();

        //await TestWaitMany();
        //await TestWaitManyFunctions();
    }

    private static async Task TestWaitManyFunctions()
    {
        //await RegisterResumableFunction(typeof(WaitManyFunctionsExample), nameof(WaitManyFunctionsExample.WaitFirstFunction));
        //await RegisterResumableFunction(typeof(WaitManyFunctionsExample), nameof(WaitManyFunctionsExample.WaitManyFunctions));
        await RegisterResumableFunction(typeof(WaitManyFunctionsExample), nameof(WaitManyFunctionsExample.WaitSubFunctionTwoLevels));
        var example = new WaitManyFunctionsExample();
        var currentProject = ProjectApprovalExample.GetCurrentProject();
        await example.ProjectSubmitted(currentProject);
        example.ManagerOneApproveProject(new ApprovalDecision(currentProject.Id, true));
        //await Task.Delay(3000);
        example.ManagerTwoApproveProject(new ApprovalDecision(currentProject.Id, true));
        example.ManagerTwoApproveProject(new ApprovalDecision(currentProject.Id, true));
        //await Task.Delay(3000);
        example.ManagerThreeApproveProject(new ApprovalDecision(currentProject.Id, true));
        //await Task.Delay(3000);
        example.ManagerThreeApproveProject(new ApprovalDecision(currentProject.Id, true));
    }
    private static async Task TestLoops()
    {
        await RegisterResumableFunction(typeof(TestLoopsExample), nameof(TestLoopsExample.WaitManagerOneThreeTimeApprovals));
        var example = new TestLoopsExample();
        var currentProject = ProjectApprovalExample.GetCurrentProject();
        await example.ProjectSubmitted(currentProject);
        example.ManagerOneApproveProject(new ApprovalDecision(currentProject.Id, true));
        example.ManagerOneApproveProject(new ApprovalDecision(currentProject.Id, true));
        example.ManagerOneApproveProject(new ApprovalDecision(currentProject.Id, true));
    }
    private static async Task TestWaitMany()
    {
        //await RegisterResumableFunction(typeof(TestWaitManyExample), nameof(TestWaitManyExample.WaitThreeMethod));
        await RegisterResumableFunction(typeof(TestWaitManyExample), nameof(TestWaitManyExample.WaitManyAndCountExpressionDefined));
        var example = new TestWaitManyExample();
        Project project = ProjectApprovalExample.GetCurrentProject();
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerTwoApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerTwoApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerThreeApproveProject(new ApprovalDecision(project.Id, true));
    }



    private static async Task TestSubFunctionCall()
    {
        await RegisterResumableFunction(typeof(ProjectApprovalExample), nameof(ProjectApprovalExample.SubFunctionTest));
        var example = new ProjectApprovalExample();
        Project project = ProjectApprovalExample.GetCurrentProject();
        example.ProjectSubmitted(project);
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerTwoApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerThreeApproveProject(new ApprovalDecision(project.Id, true));
    }

    private static async Task TestReplayGoBackAfter()
    {
        await RegisterResumableFunction(typeof(ReplayGoBackAfterExample), nameof(ReplayGoBackAfterExample.TestReplay_GoBackAfter));
        var example = new ReplayGoBackAfterExample();
        Project project = ProjectApprovalExample.GetCurrentProject();
        example.ProjectSubmitted(ProjectApprovalExample.GetCurrentProject());
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, false));
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, true));
    }

    private static async Task TestReplayGoBackBeforeNewMatch()
    {
        await RegisterResumableFunction(typeof(ReplayGoBackBeforeNewMatchExample), nameof(ReplayGoBackBeforeNewMatchExample.TestReplay_GoBackBefore));
        var example = new ReplayGoBackBeforeNewMatchExample();
        Project project = ProjectApprovalExample.GetCurrentProject();
        example.ProjectSubmitted(ProjectApprovalExample.GetCurrentProject());
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, false));
        project.Name += "-Updated";
        example.ProjectSubmitted(project);
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, true));
    }

    private static async Task RegisterResumableFunction(Type classType, string methodName)
    {
        var method =
            classType.GetMethod(methodName, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            Console.WriteLine($"No method with name `{methodName}` in class `{classType.FullName}`.");
            return;
        }
        await _scanner.RegisterResumableFunction(method, MethodType.ResumableFunctionEntryPoint);
        await _scanner.RegisterResumableFunctionFirstWait(method);
        await _scanner._context.SaveChangesAsync();
    }
}