// See https://aka.ms/new-console-template for more information

using LocalResumableFunction;
using LocalResumableFunction.Data;
using LocalResumableFunction.InOuts;
using System.Reflection;
using Test;

public class Program
{
    private static readonly Project project = ProjectApprovalExample.GetCurrentProject();
    private static Scanner _scanner;

    private static async Task Main()
    {
        _scanner = new Scanner
        {
            _context = new FunctionDataContext()
        };
        Console.WriteLine("Test App RUNNING.");


        //await TestSubFunctionCall();
        //await TestReplayGoBackAfter();
        //await TestReplayGoBackBeforeNewMatch();
       
        //await TestWaitMany();
        await TestWaitManyFunctions();

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
        await example.ProjectSubmitted(ProjectApprovalExample.GetCurrentProject());
        await Task.Delay(5000);
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerTwoApproveProject(new ApprovalDecision(project.Id, true));
        await Task.Delay(5000);
        example.ManagerThreeApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerThreeApproveProject(new ApprovalDecision(project.Id, true));
    }

    private static async Task TestWaitMany()
    {
        await RegisterResumableFunction(typeof(TestWaitManyExample), nameof(TestWaitManyExample.WaitThreeMethod));
        var example = new TestWaitManyExample();
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerTwoApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerThreeApproveProject(new ApprovalDecision(project.Id, true));
    }

    

    private static async Task TestSubFunctionCall()
    {
        await RegisterResumableFunction(typeof(ProjectApprovalExample), nameof(ProjectApprovalExample.SubFunctionTest));
        var example = new ProjectApprovalExample();
        example.ProjectSubmitted(project);
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerTwoApproveProject(new ApprovalDecision(project.Id, true));
        example.ManagerThreeApproveProject(new ApprovalDecision(project.Id, true));
    }

    private static async Task TestReplayGoBackAfter()
    {
        await RegisterResumableFunction(typeof(ReplayGoBackAfterExample), nameof(ReplayGoBackAfterExample.TestReplay_GoBackAfter));
        var example = new ReplayGoBackAfterExample();
        example.ProjectSubmitted(ProjectApprovalExample.GetCurrentProject());
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, false));
        example.ManagerOneApproveProject(new ApprovalDecision(project.Id, true));
    }

    private static async Task TestReplayGoBackBeforeNewMatch()
    {
        await RegisterResumableFunction(typeof(ReplayGoBackBeforeNewMatchExample), nameof(ReplayGoBackBeforeNewMatchExample.TestReplay_GoBackBefore));
        var example = new ReplayGoBackBeforeNewMatchExample();
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