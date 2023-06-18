# Todo

## Core functions
* New scan should not delete existing method wait and groups if exist
* Delay processing wait if the scan is in progress
* Remove direct use for DbContext
* Same DLL in two services
* Review all places where database update occurs
* Function priority
	* How hangfire handle priority
* Can I use .net standard for Handler project
* Use pull mode to get calls from a queue
* Replace HttpHangfire with abstraction to enable queue based communication
* Review todos
## Todos in code
* Handle clone time wait FirstWaitProcessor.cs	42
* review `LogErrorToService` method	FirstWaitProcessor.cs	118
* Review `CancelFunctionWaits` is suffecient	ReplayWaitProcessor.cs	42
* Recalc mandatory part	ReplayWaitProcessor.cs	89
* May cause problem for go back after	WaitProcessor.cs	236
* Validate same signature for group methods	MethodIdsRepo.cs	74
* `RemoveFirstWaitIfExist` fix this for group	WaitsRepo.cs	190
* Handle sub functions waits when cancel WaitsRepo.cs	276
* Enhance `GenericVisitor` class by override it's methods 	GenericVisitor.cs	7
* Validate input output type is serializable	MethodWait.cs	88
* Review concurrency exceptions for `WaitForCall` when update	WaitForCall.cs	3
* Should I create new scope when initialize function instance??	ResumableFunction-Wait Functions.cs	46
* Refine `GetMethodsInfo` query	ResumableFunctions.Handler	UiService.cs	161
* Set `settings.CurrentServiceUrl` here if null	Extensions.cs	31

## Enhancements
* Refactor long methods
* Parameter check lib use
* Performance Analysis
* Store options
	* Use Queue Service to Handle Pushed Calls
		* Kafka,RbbittMQ or ActiveMQ
	* Use Queue Service that support queries for fast wait insertion
* What are best practices for HTTPClient use `services.AddSingleton<HttpClient>();`

* Encryption option sensitive data
	* Function state
	* Match and SetData Expressions
* Resumable function hooks
	* After Resumed
	* On Error Occurred



# External Waits (Will be separate projects)
* Monitor network requests using reverse proxy and push MethodCalls [TCP Listener]
	* https://github.com/microsoft/reverse-proxy
* File/Folder Changes [File Watcher]
* RabbitMQ or any service bus [Subscribe to event]