# Todo

## Core functions
* Delay processing wait if the scan is in progress
* Remove direct use for DbContext
* Same DLL in two services
* Review all places where database update occurs
* Function priority
	* How hangfire handle priority
* Back to UI coding


## UI project
* RF Instance Details
	* Waits tree
		* Wait Name
		* Status
		* Is Replay
		* Extra Data
		* Wait Type
		* Created
		* If match --> Pushed method input & output
		* Expressions if method wait
		* Remove Need Load Function Data for match
		* Count Expression if group
		* Actions on Wait 
			* Cancel (If Waiting)
			* Wait Again (If Completed/Canceled)
			* Wait Again and Execute Code before (If Completed/Canceled)
			* Wait Next Wait (If Completed/Canceled)
			* Set Matched (If Waiting)
* Method Waits
* Pushed Calls Waits
* All Logs View
* Actions on service
	* Find dead methods
	* verify method signatures
	* verify start waits exist in db for each RF
	* Verify Scanned Methods 
	* Instance in progress but not wait anything check
	* Validate URN duplication when scan if diffrent method signature
	* Wait methods in same method group must have the same signature
	* Rerun faild isntance
	* Stop resumable function new instances

## Prepare how to use?
* Examples 
	* https://tallyfy.com/workflow-examples/
	* https://clickup.com/blog/workflow-examples/
* Inter services waits documentation
* Record videos

## Enhancements
* Refactor long methods
* Write unit testing for core functionallity
* Write Roslyn analyzer to force the right use for the library
* Parameter check lib use
* Performance Analysis
* Store options
	* Use Queue Service to Handle Pushed Calls
		* Kafka,RbbittMQ or ActiveMQ
* What are best practices for HTTPClient use `services.AddSingleton<HttpClient>();`
* Resumable function hooks
	* After Resumed
	* On Error Occurred
* Encryption option sensitive data
	* Function state
	* Match and SetData Expressions

## Migrate Resumable Function DB
* How to migrate resumable function database from development to production??
* What about HangfireDb

## How to Test resumable function?
* How to unit test a resumable functions
	* Generate unit test code for resumable function (I plan to auto-generate code for integration test by simulating waits in function)

# External Waits (Will be separate projects)
* Monitor network requests using reverse proxy and push MethodCalls [TCP Listener]
	* https://github.com/microsoft/reverse-proxy
* File/Folder Changes [File Watcher]
* RabbitMQ or any service bus [Subscribe to event]