# Todo

## Near
* Re-Schedule processing if there is a scan process in progress
* RF Instance Details
* Remove direct use for DbContext
* Same DLL in two services
* Test replay in sub functions
* Add BackgroundTaskAspect

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
* Review all places where database update occurs
* Refactor long methods
* Encrypt sensitive data
	* Function state
	* Match and SetData Expressions
* Write unit testing for core functionallity
* Find fast and best object serializer
	* Save function state all fields [public and non public]
	* https://github.com/quozd/awesome-dotnet#serialization
* Write Roslyn analyzer to force the right use for the library
* Parameter check lib use
* Performance Analysis
* Enable support for other store options
	* MongoDb,Tarantool or Couchbase Server
	* Kafka,RbbittMQ or ActiveMQ 
		* will be applicable for waits queuing but not states so it will be an option with use a DB
* What are best practices for HTTPClient use `services.AddSingleton<HttpClient>();`
* Resumable function hooks
	* After Resumed
	* On Error Occurred
* Speed up waits query

## Publisher Project
* Use AspectInjector instead of Fody 
* Scan and send scan result to service owner to verify signatures
* Use PeriodicTimer/Hangfire to handle background tasks
	* Send failed requests to servies
	* Scan Dlls
* DB
	* Use LiteDb to save scan Data and requests
	* Or https://github.com/hhblaze/DBreeze

## Background Cleaning Job
* Move completed/cancled function instance to Recycle Bin
	* It's logs
	* Waits
* Move completed pushed methods
* Move inactive methods identifier
* Move old logs for scan sessions
* Move soft deleted rows to recyle bin DB

## Migrate Resumable Function DB
* How to migrate resumable function database from development to production??
* What about HangfireDb

## How to Test resumable function?
* How to unit test a resumable functions
	* Generate unit test code for resumable function (I plan to be an automatic integration testing)

# External Waits (Will be separate projects)
* Monitor network requests using reverse proxy and push MethodCalls [TCP Listener]
	* https://github.com/microsoft/reverse-proxy
* RabbitMQ or any service bus [Subscribe to event]
* File/Folder Changes [File Watcher]