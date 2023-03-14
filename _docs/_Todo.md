# Todo
* Wait method in interface
* Replay GoTo with ne match
* Replay NewMatches
* IsLocked for optemistic lock
* Use hangfire to 
	* Queue pushed events requests [Fire and forget]
	* Call time based events
	* Queue matched waits for same state
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
* Run in another process
	* Save pushed waits to database or queue
* Logging and handle exception
* save expression to seprate table for duplication kill
* Create nuget package
* Test with web API
* Save function state all fields
* Find fast and best object serializer
* MoveFunctionToRecycleBin

* Parameter check lib use
* Add UI Project


* Speed Analysis	

# Test Engine Scenarios
* Seqeunce [DONE]
* Wait all [DONE]
* Wait first [DONE]
* Wait function [DONE]
* Wait many functions [DONE]
* Wait first function [DONE]
* Replay [to,after,before] for types:
	* To
	* After [DONE]
	* Before [DONE]

# Retest
* Test loops
* Test Replay

# Cancled
* No inheritance from resumable local function (Inheritance faclitate parsing and using waits)
* Remove Attributes (We can't because we need to register first waits fro a resumable function)