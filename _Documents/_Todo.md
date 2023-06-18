# Hot Todo
* New scan should not delete existing method wait and groups if exist
* Same DLL in two services
* Time wait clone if first
* Test replay

## Core functions
* Review logs to service , why first error only
* Optimize wait table indexes to enable fast wait insertion
	* Remove index 
		* [ParentWaitId] in [dbo].[Waits]
* Review check after locks
* Review activate and deactivate function
* Remove direct use for DbContext
* Review all places where database update occurs

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




# External Waits (Will be separate projects)
* Monitor network requests using reverse proxy and push MethodCalls [TCP Listener]
	* https://github.com/microsoft/reverse-proxy
* File/Folder Changes [File Watcher]
* RabbitMQ or any service bus [Subscribe to event]