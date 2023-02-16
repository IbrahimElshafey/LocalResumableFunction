// See https://aka.ms/new-console-template for more information
using LocalResumableFunction;
using LocalResumableFunction.Data;

Console.WriteLine("Test App RUNNING.");
var example = new Example();
example.ProjectSubmitted(new Project { Id = 900, Name = "Project Name", Description = "Description" });
example.ManagerOneApproveProject(new (900, true));
example.ManagerTwoApproveProject(new (900, true));
example.ManagerThreeApproveProject(new (900, true));
Console.ReadLine();