**Project Status: Work in progress [here](https://github.com/IbrahimElshafey/ResumableFunctions/tree/main/_Documents)**
* [What is Resumable Function?](#what-is-resumable-function)
* [Why this project?](#why-this-project)
* [Example Explained](#example-explained)
* [**Start using the library NuGet package**](#start-using-the-library)
* [Supported wait types and other features](#supported-wait-types-and-other-features)
* [Samples](https://github.com/IbrahimElshafey/ResumableFunctionsSamples)
* [How it works internally](#how-it-works-internally)
# What is Resumable Function?
A function that pauses and resumes execution based on other methods that it waits for them to be executed.

**Example** (this is not a pseudocode it's debuggable code):
``` C#
[ResumableFunctionEntryPoint("ClientOnboardingWorkflow.StartClientOnboardingWorkflow")]
internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
{
    yield return WaitUserRegistration();
    OwnerTaskId = service.AskOwnerToApproveClient(RegistrationResult.FormId);

    yield return WaitOwnerApproveClient();
    if (OwnerApprovalInput.Decision is false)
    {
        service.InformUserAboutRejection(RegistrationForm.UserId);
    }
    else if (OwnerApprovalInput.Decision is true)
    {
        service.SendWelcomePackage(RegistrationForm.UserId);
        ClientMeetingId = service.SetupInitalMeetingAndAgenda(RegistrationForm.UserId);

        yield return WaitMeetingResult();
        Console.WriteLine(MeetingResult);
    }

    Console.WriteLine("User Registration Done");
}
```
Each `yield return` is a place for pause/resume function execution (explained later).

# Why this project?
* The nature of server processing that depends on the fast response for efficient use of processor and memory prevents us from writing a single block of code (a method) that uses two or more long-running actions in the same code block, this means we can't translate the pseudocode below to one block of code:
```
	VactionRequest()
		when UserSubmitRequest();
		SendRequestToManager();
		wait ManagerResponse();
		DoSomeStuffAfterManagerResponse();
```
* I want to write code that reflects the business requirements so that a developer handover another without needing business documents to understand the code.
* The source code must be a source of truth about how project parts function. Handovering a project with hundreds of classes and methods to a new developer does not tell him how business flows but a resumable function will simplify understanding of what happens under the hood.
* Business functions/methods must not call each other directly, For example, the method that submits a user request should not call the manager approval method directly, a traditional solution is to create Pub/Sub services that enable the system to be loosely coupled.
*  If we used Pub/Sub loosely coupled services it will be hard to trace what happened without implementing a complex architecture.

# Example Explained
Scenario:
1. Collect client information through a registration form.
1. Send client registration for approval.
1. If rejected, inform the client about the rejection.
1. If approved, send the welcome package – email, welcome gift, etc.
1. Setup initial meeting
1. Some business after the meeting is done.


With resumable function you can write this scenario like below:
Just a few lines of codes that tells what system do.
``` C#
[ResumableFunctionEntryPoint("ClientOnboardingWorkflow.StartClientOnboardingWorkflow")]
internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
{
    yield return WaitUserRegistration();
    OwnerTaskId = service.AskOwnerToApproveClient(RegistrationResult.FormId);

    yield return WaitOwnerApproveClient();
    if (OwnerApprovalInput.Decision is false)
    {
        service.InformUserAboutRejection(RegistrationForm.UserId);
    }
    else if (OwnerApprovalInput.Decision is true)
    {
        service.SendWelcomePackage(RegistrationForm.UserId);
        ClientMeetingId = service.SetupInitalMeetingAndAgenda(RegistrationForm.UserId);

        yield return WaitMeetingResult();
        Console.WriteLine(MeetingResult);
		//Todo:some business based on meeting result
    }

    Console.WriteLine("User Registration Done");
}
```
* The resumable function must match the signature `IAsyncEnumerable<Wait> FunctionName()`
* The class that contains the resumable function must inherit `ResumableFunction`
```C#
public class ClientOnboardingWorkflow : ResumableFunction
```
* Mark a method with `[ResumableFunctionEntryPoint]` to indicate that the method paused and resumed based on waits inside
* `ResumableFunctionEntryPoint` attributes takes `string methodUrn` mandatory to track the resumable function by the library.
* `WaitUserRegistration` body is
```C#
private MethodWait<RegistrationForm, RegistrationResult> WaitUserRegistration()
{
	return Wait<RegistrationForm, RegistrationResult>("Wait User Registration", service.ClientFillsForm)
					.MatchIf((regForm, regResult) => regResult.FormId > 0)
					.SetData((regForm, regResult) => RegistrationForm == regForm && RegistrationResult == regResult);
}
```
* Wait for the `ClientFillsForm` method to be executed, this call will save an object representing the wait in the database (Wait Record) and pause the method execution until `ClientFillsForm` method called.
* We define match expression `MatchIf((regForm, regResult) => regResult.FormId > 0)` that will be evaluated when `ClientFillsForm` method called to check if it is a match for the current instance or not, The passed expression serialized and saved with the wait record in the database.
* If a match occurred (after `ClientFillsForm` called) we update the class instance data with `SetData` expression, Note that the assignment operator is not allowed in expression trees, also we save this expression in the database with the wait record.
* The execution will continue after setting data until the next wait.
* The next wait will be saved to the database in the same way.
* The resumable function library will scan your code to register first waits for each `ResumableFunctionEntryPoint`
* The library saves the class state to the database and loads it when a method called and matched.
* You must add `[WaitMethod]` attribute to the methods you want to wait,like below:
``` C#
[WaitMethod("ClientOnboardingService.ClientFillsForm")]
public RegistrationResult ClientFillsForm(RegistrationForm registrationForm)
{
	.
	.
	.
}
```
* The method marked with `[WaitMethod]` must have one input paramter that is serializable, you can use value types and strings also.
* You can mark any instance method with `[WaitMethod]` if it have one parameter.

# Start using the library
* Create new Web API project, Name it `RequestApproval`
* Check `Enable OpenApi Support`
* Change target framework to '.Net 7.0'
* Add packages (Package Manager Consol)
```
Install-package ResumableFunctions.AspNetService
Install-package Fody
Install-package MethodBoundaryAspect.Fody
```
* Add file 'FodyWeavers.xml' to project root with content
```xml
<Weavers xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="FodyWeavers.xsd">
  <MethodBoundaryAspect />
</Weavers>
```
* In `Program.cs` change:
``` C#
builder.Services.AddControllers();
```
To
```C#
builder.Services
    .AddControllers()
    .AddResumableFunctions(new ResumableFunctionsSettings().UseSqlServer());
```
* After line `var app = builder.Build();` add line `app.ScanCurrentService();`
* This configuration uses LocalDb
* In Visual Studio `View Menu` then `SQL Server Object Explorer` add empty database with name `RequestApproval_HangfireDb` to server `(localdb)\MSSQLLocalDB`
* Change `WeatherForecastController.cs` file contect with content [here](https://raw.githubusercontent.com/IbrahimElshafey/ResumableFunctionsSamples/Main/RequestApproval/Controllers/RequestApprovalController.cs).
* Rename `WeatherForecastController.cs` to `RequestApprovalController.cs`
* Register service `RequestApprovalService` in program.cs `builder.Services.AddScoped<RequestApprovalService>();`
* Set Breakpoint at lines `54,56` in file `RequestApprovalController.cs`
* Run the app and from swagger UI call `UserSubmitRequest` the breakpoint at 54 will be hit if `(Id > 0)`
* Continue excecution and copy value in `ManagerApprovalTaskId`
* From swagger call `ManagerApproval` with the `ManagerApprovalTaskId` you copied before and `Accept` for Decision prop.
* The breakpoint at 56 will be hit.
* You done.

# Supported wait types and other features
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
* You can wait a resumable sub function that is not an entry point
``` C#
 yield return Wait("Wait sub function that waits two manager approval.", WaitTwoManagers);
 ....
//method must have  `SubResumableFunction` attribute
//Must return `IAsyncEnumerable<Wait>`
[SubResumableFunction("WaitTwoManagers")]
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
* You can wait method in another service
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
* You can use time waits
``` C#
const string waitManagerOneApprovalInSeconds = "Wait manager one approval in 2 days";
yield return Wait(
        waitManagerOneApprovalInSeconds,
        new MethodWait<ApprovalDecision, bool>(ManagerOneApproveProject)
            .MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
            .SetData((input, output) => ManagerOneApproval == output),
        Wait(TimeSpan.FromDays(2))
    .SetData(() => TimerMatched == true)
).First();

if (TimerMatched)
{
    WriteMessage("Timer matched");
    TimerMatched = false;
    yield return GoBackBefore(waitManagerOneApprovalInSeconds);
}
else
{
    WriteMessage($"Manager one approved project with decision ({ManagerOneApproval})");
}
```
* Wait method in another service [browse exmaples folder in source code. I'll add docs later.]
* Register third party method by fake signature,This will enable
	* Use github web hooks for example
	* Wait for google drive file change
	* Http listener 
	* More advanced scenarios

# How it works internally
* The library uses IAsyncEnumerable generated state machine to implement a method that can be paused and resumed.
* The library saves a serialized object of the class that contains the resumable function for each resumable function instance.
* The library saves waits and pauses function execution when a wait is requested by the function.
* The library resumes function execution when a wait is matched (its method called).
