# Todo


## Prepare how to use?
* Implement workflows in 
	* https://tallyfy.com/workflow-examples/
	* https://clickup.com/blog/workflow-examples/
* Inter services waits documentation
* Record videos
* Enhance nuget package use
	*  Add FodyWeavers.xml automatic
	*  Why fody not work directlly


## UI project
* Service Details Page
	* Service Resumable Functions List
		* Resumable Function Active Instances
			* RF Instance Details
	* Service Method Wait List
* Pushed Calls List
	* Pushed Call Details
* All Logs View
* Add UI Project
	* Servcies Registered
		* Verify Scanned Methods 
		* Find methods not in code
		* verify method signatures
		* verify start waits exist in db for each RF
	* Monitor active resumable functions
		* It's props 
			* Status
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
			* Actions on Method Wait 
			* Actions on Group Wait 
			* Actions on Function Wait 
				* Cancel
				* Go Back (To,Before,After)
		* Logs list from function state logs3



## Scan Enhancements
* Validate URN duplication when scan if diffrent method signature
* Detect deleted methods
* Wait methods in same method group must have the same signature
* Verify that fody MethodBoundaryAspect is active

# Coding
* Write Roslyn analyzer to force the right use for the library
* Enable support for other store options
	* MongoDb,Tarantool or Couchbase Server
	* Kafka,RbbittMQ or ActiveMQ 
		* will be applicable for waits queuing but not states so it will be an option with use a DB
* Disable processing if there is a scan process in progress
* Refactor ResumableFunctionHandler to be multiple classes/services
	* RegisterFirstWait
	* CloneFirstWait
	* ProcessPushedCall
	* ProcessMatchedWait
* What are best practices for HTTPClient use `services.AddSingleton<HttpClient>();`


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

## Migrate Resumable Function DB
* How to migrate resumable function database from development to production??
* What about HangfireDb


## How to Test resumable function?
* How to unit test a resumable functions
	* Generate unit test code for resumable function
		

## Enhancements
* Encrypt sensitive data
	* Function state
	* Match and SetData Expressions
* Save function state all fields [public and non public]
* Find fast and best object serializer
* Parameter check lib use
* Performance Analysis

# External Waits (Will be seprate projects)
* Monitor network requests using reverse proxy and push MethodCalls [TCP Listener]
* RabbitMQ or any service bus [Subscribe to event]
* File/Folder Changes [File Watcher]


## Less Priority
* Resumable function hooks
	* Before initiating the first wait => write your cod in function
	* After initiate first wait => write your cod in function
	* After Resumed
	* After Completed => write your cod in function end
	* On Error Occurred => write your cod in function catch block
