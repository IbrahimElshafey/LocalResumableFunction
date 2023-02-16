﻿// See https://aka.ms/new-console-template for more information
using LocalResumableFunction;
using LocalResumableFunction.Data;
using Test;

Console.WriteLine("Test App RUNNING.");

//TestSubFunctionCall();
//TestReplayGoBackAfter();
TestReplayGoBackTo();
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
    example.ProjectSubmitted(new Project { Id = 1000, Name = "Project Name", Description = "Description" });
    example.ManagerOneApproveProject(new(1000, false));
    example.ManagerOneApproveProject(new(1000, true));
}
static void TestReplayGoBackTo()
{
    var example = new ReplayGoBackToExample();
    example.ProjectSubmitted(new Project { Id = 1000, Name = "Project Name", Description = "Description" });
    example.ManagerOneApproveProject(new(1000, false));
    example.ManagerOneApproveProject(new(1000, true));
}

