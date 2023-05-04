# Todo



## Prepare how to use manual
* Implement workflows in 
	* https://tallyfy.com/workflow-examples/
	* https://clickup.com/blog/workflow-examples/
* Write manual
* Record videos
* Enhance nuget package use
	*  Add FodyWeavers.xml automatic
	*  Why fody not work directlly


## UI project
* Add UI Project (Use MVC not pages)
	* Servcies Registred
		* Verify scanned methods 
		* Find methods not in code
		* verify method signatures
		* verify start waits exist in db
		* Push External Mehtod Call
	* Monitor active resumable functions
		* It's props 
			* Status
			* Name (Full Name)
			* CurrentStateObject
			* Created
			* LastUpdated
		* Waits tree
			* Wait Name
			* Status
			* Is Replay
			* Extra Data
			* Wait Type
			* Created
			* If match --> Pushed method input & output
			* Expressions if method wait
			* Need Load Function Data for match
			* Wait method
			* Count Expression if group
			* Actions on Method Wait ()
			* Actions on Group Wait ()
			* Actions on Function Wait ()
		* Logs list from function state logs
## Migrate DB
* How to migrate resumable function database from development to production??

## Scan Enhancements
* Validate URN duplication when scan if diffrent method signature
* Detect deleted methods
* Wait methods in same method group must have the same signature
* Verify that fody MethodBoundaryAspect is active
* Logging for scan sessions

## Refactoring and rewrite code
* UOW to support no-sql implementation
* Refactor ResumableFunctionHandler to be multiple classes
	* RegisterFirstWait
	* CloneFirstWait
	* ProcessPushedCall
	* ProcessMatchedWait
* what are best practices for HTTPClient use `services.AddSingleton<HttpClient>();`


## Publisher Project
* Scan and send scan result to service owner to verify signatures
* Use PeriodicTimer to handle background tasks
	* Send failed requests to servies
	* Scan Dlls
* Use LiteDb to save scan Data and requests

## Background Cleaning Job
* Move completed/cancled function instance to Recycle Bin
	* It's logs
	* Waits
* Move completed pushed methods
* Move inactive methods identifier
* Move old logs for scan sessions
* Move soft deleted rows to recyle bin DB



## How to Test resumable function?
* How to unit test a resumable functions
	* Generate unit test code for resumable function
		

## Enhancements
* Save function state all fields [public and non public]
* Find fast and best object serializer
* Parameter check lib use
* Speed Analysis	
	* https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/event-counters?tabs=windows


# External Waits (Will be seprate projects)
* Monitor network requests using reverse proxy and push MethodCalls [TCP Listener]
* WebHook for the service [Publisher Project Done]
* RabbitMQ or any service bus [Subscribe to event]
* File/Folder Changes [File Watcher]


## Less Priority
* Resumable function hooks
	* Before initiating the first wait => write your cod in function
	* After initiate first wait => write your cod in function
	* After Resumed
	* After Completed => write your cod in function end
	* On Error Occurred => write your cod in function catch block
