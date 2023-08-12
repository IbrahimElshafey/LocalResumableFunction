## Intro Video in Arabic
[![Intro Video in Arabic](https://img.youtube.com/vi/Oc9NjP0_0ig/0.jpg)](https://www.youtube.com/watch?v=Oc9NjP0_0ig)


* [PDF Intro](#resumable-function-example)
* [Resumable Function Example](#resumable-function-example)
* [Why this project?](#why-this-project)
* [**Start using the library NuGet package**](#start-using-the-library)
* [Supported Wait Types](#supported-wait-types)
* [Resumable Functions UI](https://github.com/IbrahimElshafey/ResumableFunctions/blob/main/_Documents/GitHubDocs/Resumable_Functions_UI.md)
* [Distributed Services and Resumable Function](https://github.com/IbrahimElshafey/ResumableFunctions/blob/main/_Documents/GitHubDocs/Distributed_Services_and_Resumable_Function.md)
* [How to test your resumable functions?](https://github.com/IbrahimElshafey/ResumableFunctions/blob/main/_Documents/GitHubDocs/Testing.md)
* [Configuration](https://github.com/IbrahimElshafey/ResumableFunctions/blob/main/_Documents/GitHubDocs/Configuration.md)
* [Database Cleaning Job](https://github.com/IbrahimElshafey/ResumableFunctions/blob/main/_Documents/GitHubDocs/Cleaning_Job.md)
* [Samples](https://github.com/IbrahimElshafey/ResumableFunctionsSamples)
* [How it works internally](#how-it-works-internally)

# Resumable Function Example
A resumable function is a function that can be suspended and resumed at a later point in time. This is in contrast to traditional functions, which must be executed to completion before they can return.

**Example**
![ResumableFunctionExample.png](/_Documents/GitHubDocs/IMG/ResumableFunctionExample.png)
Lines Description:
1. A resumable function must be defined in a class that inherits from `ResumableFunctionsContainer`.
1. We add the `[ResumableFunctionEntryPoint]` attribute to the resumable function to tell the library to register or save the first wait in the database when it scans the DLL for resumable functions.
1. The resumable function must return an `IAsyncEnumerable<Wait>` and must have no input parameters.
1. Each `yield return` statement is a place where the function execution can be paused until the required wait is matched, the pause may be days or months.
1. We tell the library that we want to wait for the method `_service.ClientFillsForm` to be executed. This method has an input of type `RegistrationForm` and an output of type `RegistrationResult`.
1. When the `ClientFillsForm` method is executed, the library will evaluate its input and output against the match expression. If the match expression is satisfied, the function execution will be resumed. Otherwise, the execution will not be resumed.
1. If we need to capture the input and output of the `ClientFillsForm` method after the match expression is satisfied, we can use the `AfterMatch` method.
* **The library saves the state of the resumable function in the database. This includes a serialized instance of the class that contains the resumable function, as well as any local variables.**

![PushCallAttribute.png](/_Documents/GitHubDocs/IMG/PushCallAttribute.png)
* The attribute `[PushCall]` must be added to the method you want to wait.
* The method must have one input parameter.
* This attribute will enable the method to push it's input and output to the library when it executed.
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
    Wait<Project, bool>(ProjectSubmitted, "Project Submitted")
    .MatchIf((input, output) => output == true)
    .AfterMatch((input, output) => CurrentProject = input);
```
* Wait **first method match in a group** of methods (similar to `Task.WhenAny()`)
``` C#
yield return Wait("Wait First In Three",
    Wait<string, string>(Method7, "Method 7"),
    Wait<string, string>(Method8, "Method 8"),
    Wait<string, string>(Method9, "Method 9")
).MatchAny();
```
* Wait **group of methods** to match (similar to `Task.WhenAll()`)
``` C#
yield return Wait("Wait three methods",
    Wait<string, string>(Method1, "Method 1"),
    Wait<string, string>(Method2, "Method 2"),
    Wait<string, string>(Method3, "Method 3")
    );
//or 
yield return Wait("Wait three methods",
    Wait<string, string>(Method1, "Method 1"),
    Wait<string, string>(Method2, "Method 2"),
    Wait<string, string>(Method3, "Method 3")
    ).MatchAll();
```
* **Custom wait for a group** with custom match expression that must be satisfied to mark the group as completed
``` C#
yield return Wait("Wait three methods",
    Wait<string, string>(Method1, "Method 1"),
    Wait<string, string>(Method2, "Method 2"),
    Wait<string, string>(Method3, "Method 3")
)
.MatchIf(waitsGroup => waitsGroup.CompletedCount == 2 && Id == 10 && x == 1);
```
* You can wait a **sub resumable function** that is not an entry point
``` C#
 yield return Wait("Wait sub function that waits two manager approval.", WaitTwoManagers);
 ....
//method must have  `SubResumableFunction` attribute
//Must return `IAsyncEnumerable<WaitX>`
[SubResumableFunction("WaitTwoManagers")]
public async IAsyncEnumerable<WaitX> WaitTwoManagers()
{
	//wait some code
	.
	.
	.
```
* `SubResumableFunction` Can wait another `SubResumableFunction` 
```C#
[SubResumableFunction("SubFunction1")]
public async IAsyncEnumerable<WaitX> SubFunction1()
{
    yield return Wait<string, string>(Method1, "M1").MatchAny();
    yield return Wait("Wait sub function2", SubFunction2);//this waits another resumable function
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
    Wait<string, string>(Method5, "Wait Single Method"));
```
* You can **GoBackTo** a previous wait to wait it again.
``` C#
if (ManagerOneApproval is false)
{
	WriteMessage("Manager one rejected project and replay will go to ManagerOneApproveProject.");
	yield return GoBackTo("ManagerOneApproveProject");//the name must be a previous wait
}
```
* You can **GoBackAfter** a previous wait.
``` C#
yield return
	Wait<Project, bool>(ProjectSumbitted, ProjectSubmitted)
		.MatchIf((input, output) => output == true)
		.AfterMatch((input, output) => CurrentProject = input);

await AskManagerToApprove("Manager 1",CurrentProject.Id);
yield return Wait<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
	.MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
	.AfterMatch((input, output) => ManagerOneApproval == input.Decision);

if (ManagerOneApproval is false)
{
	WriteMessage("Manager one rejected project and replay will go after ProjectSubmitted.");
	yield return GoBackAfter(ProjectSumbitted);//here is go back
}
```
* You can **GoBackBefore** a previous wait
``` C#
WriteMessage("Before project submitted.");
yield return
	Wait<Project, bool>(ProjectSumbitted, ProjectSubmitted)
		.MatchIf((input, output) => output == true && input.IsResubmit == false)
		.AfterMatch((input, output) => CurrentProject = input);

await AskManagerToApprove("Manager 1", CurrentProject.Id);
yield return Wait<ApprovalDecision, bool>("ManagerOneApproveProject", ManagerOneApproveProject)
	.MatchIf((input, output) => input.ProjectId == CurrentProject.Id)
	.AfterMatch((input, output) => ManagerOneApproval == input.Decision);

if (ManagerOneApproval is false)
{
	WriteMessage(
		"ReplayExample: Manager one rejected project and replay will wait ProjectSumbitted again.");
    //here is go back
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
    .AfterMatch(x => TimeWaitId = x.TimeMatchId);
```

# How it works internally
The library uses an [IAsyncEnumerable](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=net-7.0) generated state machine to implement a method that can be paused and resumed. An IAsyncEnumerable is a type that provides a sequence of values that can be enumerated asynchronously. A state machine is a data structure that keeps track of the current state of a system. In this case, the state machine keeps track of the current state of where function execution reached.

The library saves a serialized object of the class that contains the resumable function for each resumable function instance. The serialized object is restored when a wait is matched and the function is resumed.

One instance created each time the first wait matched for a resumable function.

If the match expression is not strict, then a single call may activate multiple waits. However, only one wait will be selected for each function. This means that pushed call will activate one instance per function, but they can activate multiple instances for different functions.

