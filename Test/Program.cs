// See https://aka.ms/new-console-template for more information
using LocalResumableFunction;
using LocalResumableFunction.Data;
using Test;

Console.WriteLine("Test App RUNNING.");

//TestSubFunctionCall();
TestReplayGoBackAfter();
//TestReplayGoBackBeforeNewMatch();
Console.ReadLine();

static void TestSubFunctionCall()
{
    var example = new Example();
    example.ProjectSubmitted(new Project { Id = 900, Name = "Project Name", Description = "Description" });
    example.ManagerOneApproveProject(new(900, true));
    example.ManagerTwoApproveProject(new(900, true));
    example.ManagerThreeApproveProject(new(900, true));
}

static void TestReplayGoBackAfter()
{
    var example = new ReplayGoBackAfterExample();
    example.ProjectSubmitted(new Project { Id = 2000, Name = "Project Name", Description = "Description" });
    example.ManagerOneApproveProject(new(2000, false));
    example.ManagerOneApproveProject(new(2000, true));
}
static void TestReplayGoBackBeforeNewMatch()
{
    var example = new ReplayGoBackBeforeExample();
    example.ProjectSubmitted(new Project { Id = 1000, Name = "Project Name", Description = "Description" });
    example.ManagerOneApproveProject(new(1000, false));
    example.ProjectSubmitted(new Project { Id = 1000, Name = "New Project", Description = "New Description" ,IsResubmit = true});
    example.ManagerOneApproveProject(new(1000, true));
}

