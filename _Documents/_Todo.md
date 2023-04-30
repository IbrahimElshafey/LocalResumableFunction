# Todo
* Compelete publisher project and test it

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

* Scan Enhancements
	* Validate URN duplication when scan if diffrent method signature
	* Detect deleted methods
	* Wait methods in same method group must have the same signature
	* Verify that fody MethodBoundaryAspect is active
	* Logging for scan sessions

* what are best practices for HTTPClient use `services.AddSingleton<HttpClient>();`
* Create nuget package

* Background Cleaning Job
	* Move completed/cancled function instance to Recycle Bin
		* It's logs
		* Waits
	* Move completed pushed methods
	* Move inactive methods identifier
	* Move old logs for scan sessions
	* Move soft deleted rows to recyle bin DB

* Resumable function hooks
	* Before initiating the first wait
	* After initiate first wait
	* After Resumed
	* After Completed
	* On Error Occurred


* How to unit test a resumable functions
	* Generate unit test code for resumable function
		


* Save function state all fields [public and non public]
* Find fast and best object serializer
* Parameter check lib use

* Speed Analysis	
	* https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/event-counters?tabs=windows


# External Waits (Will be seprate projects)
* Monitor network requests using reverse proxy and push MethodCalls [TCP Listener]
* WebHook for the service [Publisher Project]
* RabbitMQ or any service bus [Subscribe to event]
* File/Folder Changes [File Watcher]
