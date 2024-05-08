# Features
* Find instances that failed when resume execution and re-run them
* Build Nuget package to target multiple runtimes and versions
* Security and IUserContext

* Function priority/Matched Waits priority
	* How hangfire handle priority

* Performance Analysis/Monitoring
	* Pushed calls that is not necessary
	* How long processing a pushed call takes?
* Encryption option for sensitive data
	* Function state
	* Match and SetData Expressions
* Services registry is separate service

* ResumableFunctionsController must be relay on abstraction for IExternalCallHandler
	* IExternalCallHandler may relay as http service
	* IExternalCallHandler may relay as gRPC service
	* We need to use RF in any service or process

