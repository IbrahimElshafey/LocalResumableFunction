// See https://aka.ms/new-console-template for more information
using LocalResumableFunction;
using LocalResumableFunction.Data;

Console.WriteLine("Test App RUNNING.");
var example = new Example();
example.ProjectSubmitted(new Project { Id = 500, Name = "Project Name", Description = "Description" });
example.ManagerApproveProject((500, true));
Console.ReadLine();