# Todo
* Delete PushedMethodsCalls after processing 
* HangFireHttpClient
* Activate on start wait if multiple exist
* TimeWaits background task using hangfire test
* Hangfire access dbcontext problem [solved with transiet but need test]
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
	* List completed functions
	* Verify scanned methods 
		* delete methods not in code
		* verify method signatures
		* verofy start waits exist in db
	* Register External Method
		* For example github web hook


* Speed Analysis	
