# Todo
* Add 
	* IsLocked for optemistic lock
	
* Validate waits
	* Wait same wait twice in group error
	* Wait name can't duplicate in same method

* Centralize state database
	* Use many database providers [Postgres,Sql Server]
	* Use Database as a service with OData implementation

* Wait for external method
	* Add interface with methods
	* Mark intefaces with [WaitExternalMethods] attribute that takes
		* AssemblyName
		* ClassName
		* MethodName [as method name itself]
		* MethodSignature [as method sigature itself]
	* External method will be pushed in custom way



* Remove uniqe for method hash

* Logging and handle exception

* Create nuget package

* Save function state all fields [public and non public]
* Find fast and best object serializer
* MoveFunctionToRecycleBin

* Parameter check lib use
* Add UI Project


* Speed Analysis	
