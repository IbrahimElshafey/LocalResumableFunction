# Todo

* Handle concurrency problems
* Compelete publisher project and test it
* Wait methods in same group must have the same signature

* Verify that fody MethodBoundaryAspect is active

* Logging for scan sessions

* Background Cleaning Job
	* Move completed/cancled function instance to Recycle Bin
		* It's logs
		* Waits
	* Move completed pushed methods
	* Move inactive methods identifier
	* Move old logs for scan sessions

* Resumable function hooks
	* Before initiating the first wait
	* After initiate first wait
	* After Resumed
	* After Completed
	* On Error Occurred

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

* Create nuget package


* Handle power shut down

* How to unit test a resumable function
	* Generate unit test code for resumable function
* Generate external service class form (swagger defention)
		
* Delete first wait subwaits if group

* Activate one start wait if multiple exist for same method

* Save function state all fields [public and non public]
* Find fast and best object serializer

* Parameter check lib use

* Speed Analysis	
	* https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/event-counters?tabs=windows


# External Waits 
* Monitor network requests using reverse proxy and push MethodCalls [TCP Listener]
* WebHook for the service [Publisher Project]
* RabbitMQ or any service bus [Subscribe to event]
* File/Folder Changes [File Watcher]
