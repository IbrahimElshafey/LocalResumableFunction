# Todo
* Add UI Project
	* Monitor active resumable functions
		* Incoming waits
		* Past waits
		* Status
	* List completed functions
	* Verify scanned methods 
		* delete methods not in code
		* verify method signatures
		* verify start waits exist in db
	* Register External Method
		* For example github web hook

* Logging for scan sessions

* Delete first wait subwaits if group
* If scan error occured don't update service data LastScanDate




* Activate one start wait if multiple exist for same method

* Handle concurrency problems
	* optimistic or pessimistic for cases below:
	* Two waits matched for same FunctionState
	* First wait closed but new request come before create new one
	* Update pushed methods calls counter
	* Database.EnsureCreated(); in same time





* Create nuget package

* Save function state all fields [public and non public]
* Find fast and best object serializer
* Move completed function instance to Recycle Bin
* Delete PushedMethodsCalls after processing background job
* Parameter check lib use



* Speed Analysis	
	* https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/event-counters?tabs=windows