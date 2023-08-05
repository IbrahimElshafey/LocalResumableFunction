## Intro Video in Arabic
[![Intro Video in Arabic](https://img.youtube.com/vi/Oc9NjP0_0ig/0.jpg)](https://www.youtube.com/watch?v=Oc9NjP0_0ig)


* [What are Resumable Functions?](#what-are-resumable-functions)
* [Why this project?](#why-this-project)
* [Example](#example)
* [**Start using the library NuGet package**](#start-using-the-library)
* [Supported Wait Types](#supported-wait-types)
* [Resumable Functions UI](https://github.com/IbrahimElshafey/ResumableFunctions/blob/main/_Documents/GitHubDocs/Resumable_Functions_UI.md)
* [Distributed Services and Resumable Function](https://github.com/IbrahimElshafey/ResumableFunctions/blob/main/_Documents/GitHubDocs/Distributed_Services_and_Resumable_Function.md)
* [How to test your resumable functions?](https://github.com/IbrahimElshafey/ResumableFunctions/blob/main/_Documents/GitHubDocs/Testing.md)
* [Configuration](https://github.com/IbrahimElshafey/ResumableFunctions/blob/main/_Documents/GitHubDocs/Configuration.md)
* [Database Cleaning Job](https://github.com/IbrahimElshafey/ResumableFunctions/blob/main/_Documents/GitHubDocs/Cleaning_Job.md)
* [Samples](https://github.com/IbrahimElshafey/ResumableFunctionsSamples)
* [How it works internally](#how-it-works-internally)
# What Are Resumable Functions?
A resumable function is a function that can be suspended and resumed at a later point in time. This is in contrast to traditional functions, which must be executed to completion before they can return.

In the example below each [yield return] is a place for pause/resume function execution (explained later).

**Example** (this is not a pseudocode it's debuggable code):
``` C#
[ResumableFunctionEntryPoint("ClientOnboardingWorkflow.StartClientOnboardingWorkflow")]
internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
{
    yield return WaitUserRegistration();
    OwnerTaskId = _service.AskOwnerToApproveClient(RegistrationResult.FormId);

    yield return WaitOwnerApproveClient();
    if (OwnerApprovalInput.Decision is false)
    {
        _service.InformUserAboutRejection(RegistrationForm.UserId);
    }
    else if (OwnerApprovalInput.Decision)
    {
        _service.SendWelcomePackage(RegistrationForm.UserId);
        ClientMeetingId = _service.SetupInitalMeetingAndAgenda(RegistrationForm.UserId);

        yield return WaitMeetingResult();
        Console.WriteLine(MeetingResult);
    }

    Console.WriteLine("User Registration Done");
}
```

# Why this project?
Server processing must be fast to be efficient with processor and memory resources. This means that we can't write a method that blocks for a long time, such as days. For example, the following pseudocode cannot be translated into a single block of code:
```
VactionRequest()
    wait UserSubmitRequest();
    SendRequestToManager();
    wait ManagerResponsetoTheRequest();
    DoSomeStuffAfterManagerResponse();
```
We want to write code that reflects the business requirements so that a developer can hand it off to another developer without needing business documents to understand the code. The source code must be a source of truth about how project parts operate. Handing off a project with hundreds of classes and methods to a new developer doesn't tell them how business flows, but a resumable function will simplify understanding of what happens under the hood.

Business functions/methods must not call each other directly. For example, the method that submits a user request should not call the manager approval method directly. A traditional solution is to create Pub/Sub services that enable the system to be loosely coupled. However, if we used Pub/Sub loosely coupled services, it would be hard to trace what happened without implementing a complex architecture and it will be very hard to get what happen when after an action completed.

This project aims to solve the above problems by using resumable functions. Resumable functions are functions that can be paused and resumed later. This allows us to write code that reflects the business requirements without sacrificing readability. It makes it easier to write distributed systems/SOA/Micro Services that are easy to understand when a developer reads the source code.

This project makes resumable functions a reality.

# Example
The example below shows how to use resumable functions to implement a client onboarding workflow. The workflow consists of the following steps:
1. The user fills out a registration form.
1. The system sends the registration for approval by owner.
1. If the registration is approved, the system sends the welcome package.
1. If the registration is rejected, the system sends a message to the user about rejection.
1. The system sets up an initial meeting.
1. Some business after the meeting is done.


With resumable function you can write this scenario like below:
Just a few lines of codes that tells what happen!
``` C#
[ResumableFunctionEntryPoint("ClientOnboardingWorkflow.StartClientOnboardingWorkflow")]
internal async IAsyncEnumerable<Wait> StartClientOnboardingWorkflow()
{
    yield return WaitUserRegistration();
    OwnerTaskId = _service.AskOwnerToApproveClient(RegistrationResult.FormId);

    yield return WaitOwnerApproveClient();
    if (OwnerApprovalInput.Decision is false)
    {
        _service.InformUserAboutRejection(RegistrationForm.UserId);
    }
    else if (OwnerApprovalInput.Decision)
    {
        _service.SendWelcomePackage(RegistrationForm.UserId);
        ClientMeetingId = _service.SetupInitalMeetingAndAgenda(RegistrationForm.UserId);

        yield return WaitMeetingResult();
        Console.WriteLine(MeetingResult);
    }

    Console.WriteLine("User Registration Done");
}
```
* The resumable function must match the signature `IAsyncEnumerable<Wait> FunctionName()`
* The class that contains the resumable function must inherit `ResumableFunctionsContainer` class.
```C#
public class ClientOnboardingWorkflow : ResumableFunctionsContainer
```
* Mark a method with `[ResumableFunctionEntryPoint]` to indicate that the method pauses and resumes based on waits inside.
* The library will scan your DLL to find first waits for each resumable function to register/save it in the database.
* `ResumableFunctionEntryPoint` attributes take `string methodUrn` mandatory to track the resumable function by the library so you can change the method name if you want.
* The method `WaitUserRegistration` return a wait like below:
```C#
private MethodWait<RegistrationForm, RegistrationResult> WaitUserRegistration()
{
    return Wait<RegistrationForm, RegistrationResult>(_service.ClientFillsForm, "Wait User Registration")
            .MatchIf((regForm, regResult) => regResult.FormId > 0)
            .SetData((regForm, regResult) =>
                RegistrationForm == regForm &&
                RegistrationResult == regResult);
}
```
* The code translation is, We wait for the `_service.ClientFillForm` method to be called and finished, and if the match condition matches then the resumable function will resume.
* Library will save a wait record for `_service.ClientFillForm` in database and pause the resumable function execution until `ClientFillsForm` method called.
* `ClientFillsForm` method must be taged with attribute `[PushCall]`
```C#
[PushCall("ClientOnboardingService.ClientFillsForm")]
public RegistrationResult ClientFillsForm(RegistrationForm registrationForm)
{
    //method body
}
```
* The attribute `[PushCall]` enables the method to push its input and output when it successfully called and completed.
* We define match expression `MatchIf((regForm, regResult) => regResult.FormId > 0)` that will be evaluated when `ClientFillsForm` method called to check if it is a match for the current instance or not.
* When a call pushed to the library,the library search for active waits that may be a match.
* The library serializes the class containing the resumable function, saves it to the database, and loads it when a method is called and matched.
* The match expression evaluated against the method input and output and active instance data.
* If a match occurred we update the class instance data using `SetData` expression, Note that the assignment operator is not allowed in expression trees so we use equal operator.
* The execution will continue after setting data until the next wait or finishing.
* The next wait will be saved to the database in the same way.
* The loop will continue until resumable function end.0
* The method marked with `[PushCall]` must have one input paramter that is serializable, you can use value types and strings also.
* You can mark any instance method with `[PushCall]` if it have one parameter.
* We use [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp) to serialize/deserialize ResumableFunctionsContainer instance.

# Start using the library
* Create new Web API project, Name it `RequestApproval`
* Check `Enable OpenApi Support`
* Change target framework to '.Net 7.0'
* Install Package
```
Install-package ResumableFunctions.AspNetService
```
* In `Program.cs` change:
``` C#
builder.Services.AddControllers();
```
To
```C#
builder.Services
    .AddControllers()
    .AddResumableFunctions(
        new SqlServerResumableFunctionsSettings()
        .SetCurrentServiceUrl("<current-service-url>"));
```
* After line `var app = builder.Build();` add line `app.UseResumableFunctions();`
* This configuration uses LocalDb to store waits data.
* This configuration uses [Hangfire](https://github.com/HangfireIO/Hangfire) for background processing.
* You now can write a resumable functions in your service.
* See samples [here](https://github.com/IbrahimElshafey/ResumableFunctionsSamples) 

# Supported Wait Types
* Wait **single method** to match (similar to `await` in `async\await`)
``` C#
 yield return
         Wait<Project, bool>("Project Submitted", ProjectSubmitted)
             .MatchIf((input, output) => output == true)
             .SetData((input, output) => CurrentProject == input);
```
* Wait **first method match in a group** of methods (similar to `Task.WhenAny()`)
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
* Wait **group of methods** to match (similar to `Task.WhenAll()`)
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
* **Custom wait for a group** with custom match expression that must be satisfied to mark the group as completed
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
* You can wait a **sub resumable function** that is not an entry point
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
```C#
[SubResumableFunction("SubFunction1")]
public async IAsyncEnumerable<Wait> SubFunction1()
{
    yield return Wait<string, string>(Method1, "M1").MatchAll();
    yield return Wait("Wait sub function2", SubFunction2);
}
```
* You can wait **mixed group** that contains `SubResumableFunction`s, `MethodWait`s and `WaitsGroup`s
```C#
yield return Wait("Wait Many Types Group",
    Wait("Wait three methods in Group",
        Wait<string, string>(Method1, "Method 1"),
        Wait<string, string>(Method2, "Method 2"),
        Wait<string, string>(Method3, "Method 3")
    ),
    Wait("Wait sub function", SubFunction),
    Wait<string, string>(Method5, "Wait Method M5"));
```
* You can **GoBackTo** a previous wait to wait it again.
``` C#
if (ManagerOneApproval is false)
{
	WriteMessage("Manager one rejected project and replay will go to ManagerOneApproveProject.");
	yield return GoBackTo("ManagerOneApproveProject");
}
```
* You can **GoBackAfter** a previous wait.
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
* You can **GoBackBefore** a previous wait
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
* You can use **time waits**
``` C#
yield return
    Wait(TimeSpan.FromDays(2), "Wait Two Days")
    .SetData(x => TimeWaitId == x.TimeMatchId);
```

# How it works internally
The library uses an [IAsyncEnumerable](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=net-7.0) generated state machine to implement a method that can be paused and resumed. An IAsyncEnumerable is a type that provides a sequence of values that can be enumerated asynchronously. A state machine is a data structure that keeps track of the current state of a system. In this case, the state machine keeps track of the current state of where function execution reached.

The library saves a serialized object of the class that contains the resumable function for each resumable function instance. The serialized object is restored when a wait is matched and the function is resumed.

One instance created each time the first wait matched for a resumable function.

If the match expression is not strict, then a single call may activate multiple waits. However, only one wait will be selected for each function. This means that pushed call will activate one instance per function, but they can activate multiple instances for different functions.

