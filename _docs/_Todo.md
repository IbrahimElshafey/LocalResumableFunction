# Todo
* Validate waits
	* Wait same wait twice in group is not valid
	* Wait name can't duplicate in same method
* Time wait exact match
* Resumable function in another  dll that not entry assembly
* Activate one start wait if multiple exist for same method
* Add IsLocked for optemistic lock for state
* Handle concurrency problems
	* Two waits matched for same FunctionState
	* First wait closed but new request come before create new one
	* Update pushed methods calls counter


* Track code changes
	* GUID for methods for easy track 

* Remove uniqe for method hash

* Logging and handle exception

* Create nuget package


* Save function state all fields [public and non public]
* Find fast and best object serializer
* Move completed function instance to Recycle Bin
* Delete PushedMethodsCalls after processing background job
* Parameter check lib use
* Add UI Project
	* Monitor active resumable functions
		* Incoming waits
		* Past waits
		* Status
	* List completed functions
	* Verify scanned methods 
		* delete methods not in code
		* verify method signatures
		* verofy start waits exist in db
	* Register External Method
		* For example github web hook


* Speed Analysis	
	* https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/event-counters?tabs=windows
