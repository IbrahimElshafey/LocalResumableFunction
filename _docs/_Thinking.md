# Interscervices Waits
* There is no broker/mediator
	* If method called on service X and some waits matched:
		* If wait in same service -> check match function and set data the resume function execution
		* If wait in external service -> mark wait as matched and notify the external service
	* Every serive handle it's own waits
	* If wait is not owned by current service it will notify the owner service to handle it



* Use background tasks instead of hangfire
	* Time wait 
	* Fire and forget push method called
	* Periodic check for unhandled matched waits
* https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio


* Service
	* AddFunctionEngine
	* AddEventProvider
	* EngineSettings (DataProvider,ConnectionString,ServiceUrl)
	* Background Timer Service
	* Background Handle Pushes Service
		* Queue sevice
		* Periodic
		* Can be notified by external
		* Queue matched waits for same state