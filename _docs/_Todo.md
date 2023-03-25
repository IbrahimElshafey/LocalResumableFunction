# Todo
* Working on
	* Scanner Add external methods
	* Call http client
* Filter matched waits to be unique to solve activate same wait multiple times by same method call
* TimeWaits background task using hangfire test
* Hangfire access dbcontext problem
	* A second operation was started on this context instance before a previous operation completed. This is usually caused by different threads concurrently using the same instance of DbContext. For more information on how to avoid threading issues with DbContext, see https://go.microsoft.com/fwlink/?linkid=2097913.
* Add IsLocked for optemistic lock for state
	
* Validate waits
	* Wait same wait twice in group is not valid
	* Wait name can't duplicate in same method


		



* Remove uniqe for method hash

* Logging and handle exception

* Create nuget package

* Save function state all fields [public and non public]
* Find fast and best object serializer
* MoveFunctionToRecycleBin

* Parameter check lib use
* Add UI Project
	* Monitor active resumable functions
		* Incoming wiats
		* Past waits
		* Status
	* Verify scanned methods 
		* delete methods not in code
		* verify method signatures
		* verofy start waits exist in db
	* Register External Method
		* For example github web hook


* Speed Analysis	
