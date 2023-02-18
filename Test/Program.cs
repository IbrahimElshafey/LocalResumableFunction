// See https://aka.ms/new-console-template for more information
using LocalResumableFunction;
using LocalResumableFunction.Data;
using Test;

public class Program
{
    static void Main()
    {
        Console.WriteLine("Test App RUNNING.");


        //TestSubFunctionCall();
        //TestReplayGoBackAfter();
        //TestReplayGoBackBeforeNewMatch();
        TestWaitMany();
        Console.ReadLine();
    }

    static Project project = Example.GetCurrentProject();

    static void TestWaitMany()
    {
        var example = new TestWaitManyExample();
        example.ManagerOneApproveProject(new(project.Id, true));
        example.ManagerTwoApproveProject(new(project.Id, true));
        example.ManagerThreeApproveProject(new(project.Id, true));
    }

    static void TestSubFunctionCall()
    {
        var example = new Example();
        example.ProjectSubmitted(project);
        example.ManagerOneApproveProject(new(project.Id, true));
        example.ManagerTwoApproveProject(new(project.Id, true));
        example.ManagerThreeApproveProject(new(project.Id, true));
    }

    static void TestReplayGoBackAfter()
    {
        var example = new ReplayGoBackAfterExample();
        example.ProjectSubmitted(Example.GetCurrentProject());
        example.ManagerOneApproveProject(new(project.Id, false));
        example.ManagerOneApproveProject(new(project.Id, true));
    }
    static void TestReplayGoBackBeforeNewMatch()
    {
        var example = new ReplayGoBackBeforeNewMatchExample();
        example.ProjectSubmitted(Example.GetCurrentProject());
        example.ManagerOneApproveProject(new(project.Id, false));
        project.Name += "-Updated";
        example.ProjectSubmitted(project);
        example.ManagerOneApproveProject(new(project.Id, true));
    }
}
