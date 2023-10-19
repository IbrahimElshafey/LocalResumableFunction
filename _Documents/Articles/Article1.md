# What are resumable functions and how?
Envision having a capability that enables us to wait for a method to execute over extended periods, spanning hours, days, or even months, all without burdening the CPU or causing excessive memory consumption. Such a feature would represent a profound leap forward in the way we approach software development.

As an example, if we were designing a method to handle a vacation request workflow, it might look something like this:
```
wait VacationRequestSubmitted()
SendRequestToManager()
wait ManagerResponse()
SendEmailToVacationRequester()
```
In this context, the "wait" statement has the potential to suspend the execution for a significant duration, possibly ranging from days to even months, depending on the precise demands and timelines associated with the vacation request workflow.

To make resumable function like the above work, you would need to:
	* Save the State: Save the state of the method's local variables and the containing class to a database.
	* Record Execution Point: Record the precise execution point in the method where it was paused. This allows you to identify where the method should continue from.
	* Message Trigger: When the awaited method (the one you are waiting for) is eventually invoked, it could send a message to a processing engine indicating that it has been executed.
	* Processing Engine Handling: The processing engine is responsible for managing resumable functions. It scans for any pending "waits" linked to the method that sent the message.
	* State Loading: Once the processing engine identifies a pending "wait" for that specific method, it can load the saved state from the database, including variable values and the execution point.
	* Resuming Execution: With the saved state in hand, the processing engine can continue the execution of the resumable function from the exact point where it was previously interrupted.

This approach offers a robust mechanism for managing long-running processes and has the potential to greatly enhance software development practices.

The Resumable Functions Library I've created effectively encapsulates the set of steps I mentioned in the previous point.

# Why resumable functions matter?
In the earlier pseudocode, we didn't directly call the VacationRequestSubmitted method; instead, we were waiting for it to be triggered. In this design, the VacationRequestSubmitted method's sole responsibility is to save the request to the database, without any knowledge of the subsequent steps in the process. This architectural approach ensures a rapid response when a user initiates the submission, such as by clicking a "Submit" button on a vacation request form. By decoupling the steps in this manner, you can enhance the responsiveness of each individual component that directly engages with the user. This separation of concerns and responsibilities can lead to more efficient and maintainable code.

The second noteworthy feature is that developers can now easily comprehend the workflow by examining a single method, as demonstrated above. When they need to update the code to align with changing business requirements, they can readily pinpoint the areas that require modifications. For instance, if there's a necessity to send the vacation request to both the manager and the HR manager, the code can be adapted as follows:
```
wait VacationRequestSubmitted()
SendRequestToManager()
wait (ManagerResponse(), HrResponse())
SendEmailToVacationRequester()
```
This enhances the code's readability and maintainability, enabling developers to efficiently make adjustments as business requirements evolve.

Methods such as VacationRequestSubmitted, ManagerResponse, and HrResponse may indeed exist in external services, and when they are executed, they send or push a message to the processing engine. In your code, you can then wait for these methods to respond unless they are located in other services. This approach can certainly inject excitement into the development of distributed systems, making the process more efficient and well-organized.

By centralizing the handling of interactions with these external services and orchestrating them within your codebase, you can simplify the complexities associated with distributed systems. This approach can result in a more streamlined development experience and help you better coordinate tasks across various components of your system.


# Introducing the Resumable Functions Library
I have developed a C# library called "resumable functions" that allows you to create functions or methods with the unique capability of being paused or suspended when they encounter a "wait" method execution request. These resumable functions stay in a suspended state until the corresponding "wait" method is invoked, at which point they seamlessly resume their execution from the exact point where they were previously halted.

This article will provide an overview of what resumable functions are and how to use this library effectively. In next topics I will explain in details:
* How resumable functions works internaly
* Wait Types
* How To wait for a method in external service
* Resumable functions logging
* Serialization in resumable functions
* How to write tests for your resumable function
* What are my next steps?