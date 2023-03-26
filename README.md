**Project Status: Work in progress**

# What is Resumable Function?
A function that pauses and resumes execution based on external method/s that it waits for it to be executed.

# Code example 

``` C#
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
```
* **Point 1:** Mark a method with `[ResumableFunctionEntryPoint]` to indicate that the method paused and resumed based on waits inside
* **Point 2:** Wait for the `ProjectSubmitted` method to be executed, this call will save an object representing the wait in the database (Wait Record) and pause the method execution until `ProjectSubmitted` method called.
* **Point 3:** We pass an expression tree `(project, output) => output && project.IsResubmit == false` that will be evaluated when `ProjectSubmitted` method called to check if it is a match for the current instance or not, The passed expression serialized and saved with the wait record in the database.
* **Point 4:** If a match occurred we update the class instance data with `SetData` expression, Note that the assignment operator is not allowed in expression trees, also we save this expression in the database with the wait record.
* The execution will continue after the match until the next wait.
* The next wait will be saved to the database in the same way.
* The resumable function library will scan your code to register first waits for each `ResumableFunctionEntryPoint`
* The library saves the class state to the database and loads it when a method called and matched.
* You must add `[WaitMethod]` attribute to the methods you want to wait.
``` C#
[WaitMethod]
internal async Task<bool> ProjectSubmitted(Project project)
{
	.
	.
	.
[WaitMethod]
public bool ManagerOneApproveProject(ApprovalDecision args)
{
	.
	.
	.
```
* The method marked with `[WaitMethod]` must have one input paramter that is serializable.
* you can mark any instance method with `[WaitMethod]` if it have one parameter.

# Why this project?
* I want to write code that reflects the business requirements so that a developer handover another without needing business documents to understand the code.
* Most workflow engines can't be extended to support complex scenarios, for example, the below link contains a list of workflow patterns, which are elementary to implement by any developer if we just write code and not think about how communications work.
	http://www.Functionpatterns.com/patterns/
* The source code must be a source of truth about how project parts function, and handover a project with hundreds of classes and methods to a new developer does not tell him what business flow executed but a resumable function will simplify understanding of what happens under the hood.
*  If we used Pub/Sub loosely coupled services it willbe hard to trace what happened without implementing a complex architecture.

# Supported Wait Types
* Wait single method to match (similar to `await` in `async\await`)
``` C#
 yield return
         Wait<Project, bool>("Project Submitted", ProjectSubmitted)
             .MatchIf((input, output) => output == true)
             .SetData((input, output) => CurrentProject == input);
```
* Wait first method match in a group of methods (similar to `Task.WhenAny()`)
``` C#
 yield return Wait(
            "Wait first in two",
            new MethodWait<Project, bool>(ProjectSubmitted)
                .MatchIf((input, output) => output == true)
                .SetData((input, output) => CurrentProject == input),
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output)
        ).First();
```
* Wait group of methods to match (similar to `Task.WhenAll()`)
``` C#
 yield return Wait(
            "Wait three methods",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output)
        ).All();
```
* Custom wait for a group
``` C#
 yield return Wait(
            "Wait many with complex match expression",
            new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerOneApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerTwoApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerTwoApproval == output),
            new MethodWait<ApprovalDecision, bool>(ManagerThreeApproveProject)
                .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
                .SetData((input, output) => ManagerThreeApproval == output)
        ).When(waitGroup => waitGroup.CompletedCount == 2);//wrtite any complex exprssion against waitGroup
```
* You can wait a resumable sub function that is not and entry point
``` C#
 yield return Wait("Wait sub function that waits two manager approval.", WaitTwoManagers);
 ....
//method must have  `SubResumableFunction` attribute
//Must return `IAsyncEnumerable<Wait>`
[SubResumableFunction]
public async IAsyncEnumerable<Wait> WaitTwoManagers()
{
	//wait some code
	.
	.
	.
```
* `SubResumableFunction` Can wait another `SubResumableFunction` 
* You can wait multiple `SubResumableFunction`s
* You can wait mixed group that contains `SubResumableFunction`s, `MethodWait`s and `WaitsGroup`s
* You can GoBackTo a previous wait to wait it again.
``` C#
if (ManagerOneApproval is false)
{
	WriteMessage("Manager one rejected project and replay will go to ManagerOneApproveProject.");
	yield return GoBackTo("ManagerOneApproveProject");
}
```
* You can GoBackAfter a previous wait.
``` C#
yield return
	Wait<Project, bool>(ProjectSumbitted, ProjectSubmitted)
		.MatchIf((input, output) => output == true)
		.SetData((input, output) => CurrentProject == input);

await AskManagerToApprove("Manager 1",CurrentProject.Id);
yield return Wait<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
	.MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
	.SetData((input, output) => ManagerOneApproval == input.Decision);

if (ManagerOneApproval is false)
{
	WriteMessage("Manager one rejected project and replay will go after ProjectSubmitted.");
	yield return GoBackAfter(ProjectSumbitted);
}
```
* You can GoBackBefore a previous wait
``` C#
WriteMessage("Before project submitted.");
yield return
	Wait<Project, bool>(ProjectSumbitted, ProjectSubmitted)
		.MatchIf((input, output) => output == true && input.IsResubmit == false)
		.SetData((input, output) => CurrentProject == input);

await AskManagerToApprove("Manager 1", CurrentProject.Id);
yield return Wait<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
	.MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
	.SetData((input, output) => ManagerOneApproval == input.Decision);

if (ManagerOneApproval is false)
{
	WriteMessage(
		"ReplayExample: Manager one rejected project and replay will wait ProjectSumbitted again.");
	yield return
		GoBackBefore<Project, bool>(
			ProjectSumbitted,
			(input, output) => input.Id == CurrentProject.Id && input.IsResubmit == true);
}
```
* You can mark interface method with `[WaitMethod]` and in this case the implementation must have the attribute `[WaitMethodImplementation]`
``` C# 
internal interface IManagerFiveApproval
{
	[WaitMethod]
	bool ManagerFiveApproveProject(ApprovalDecision args);
}
....
//in class implementation
[WaitMethodImplementation]
public bool ManagerFiveApproveProject(ApprovalDecision args)
{
	WriteAction($"Manager Four Approve Project with decision ({args.Decision})");
	return args.Decision;
}
```
* [Working on waiting method in another service]
``` C#
//you will create empty implementation for method you want to wait from the external
public class ExternalServiceClass
{
	//The [ExternalWaitMethod] attribute used to exactly point to external method you want to wait
	//The class name is the full class name in the external service
	//The AssemblyName is the assembly name for the external service
	//The method name must be the same as the on in the external service
	//The method return type name and input type name must be the same as the on in the external service
	[ExternalWaitMethod(ClassName = "External.IManagerFiveApproval",AssemblyName ="SomeAssembly")]
	public bool ManagerFiveApproveProject(ApprovalDecision args)
	{
		return default;
	}
}
/// you can wait it in your code normally
yield return
	Wait<ApprovalDecision, bool>("Manager Five Approve Project External Method", 
	new ExternalServiceClass().ManagerFiveApproveProject)//here
		.MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
		.SetData((input, output) => ManagerFiveApproval == output);
```
* Register third party method by fake signature,This will enable
	* Use github web hooks fro example
	* Wait for google drive file change
	* Http listener 
	* More advanced scenarios

# How it works internally
* The library uses IAsyncEnumerable generated state machine to implement a method that can be paused and resumed.
* The library saves a serialized object of the class that contains the resumable function for each resumable function instance.
* The library saves waits and pauses function execution when a wait is requested by the function.
* The library resumes function execution when a wait is matched (its method called).

# Simple resumable function scenario 
* If we assumed a very simple scenario where someone submits a request and Manager1, Manager2 and Manager3 approve the request sequentially.
* If we implement this as an API without any messaging (message broker) then we will have actions (SumbitRequest, AskManager_X_Approval, Manager_X_SumbitApproval)
* Each of these actions will call the other based on the Function, And if the Function changes to another scenario (for example send the request to the three managers in parallel and wait for them all) we must update these actions in different places.
* Using a messaging bus instead of direct calls does not tell us how the Function goes and didn't solve the sparse update problem.
* Using a commercial Function engine is expensive and as a rule, any technology that is drag and drop will not solve the problem because we can't control every bit.

# Search for a solution results
I evaluated the existing solutions and found that there is no solution that fits all scenarios,I found that D-Async is the best for what I need but I need a more simple generic solution.
* [D-Async (The best)](https://github.com/Dasync/Dasync)
* [MassTransit](https://masstransit-project.com/)
* [Durable Task Framework](https://github.com/Azure/durabletask)
* [Workflow Core](https://github.com/danielgerlag/workflow-core)
* [Infinitic (Kotlin)](https://github.com/infiniticio/infinitic)
