# Hot Todo Core functions
* Same DLL in two services
* Error codes for logs
* [Refactor and Enhancements.md]
* If register first wait failed for function make it halted
* If service scan failed make it halted
* Review that new scan should not delete existing methods and groups if exist

* Review activate and deactivate function
* Use IMaterializationInterceptor to set entity dependencies
* Review check after locks
* Remove direct use for DbContext
* Review all places where database update occurs
* Sql server settings (password,username) or connection string

## Todos in code
* `RemoveFirstWaitIfExist` fix this for group	WaitsRepo.cs	190
* Review `CancelFunctionWaits` is suffecient	ReplayWaitProcessor.cs	42
* Recalc mandatory part	ReplayWaitProcessor.cs	89
* May cause problem for go back after	WaitProcessor.cs	236
* Validate same signature for group methods	MethodIdsRepo.cs	74
* Handle sub functions waits when cancel WaitsRepo.cs	276
* Validate input output type is serializable	MethodWait.cs	88
* Review concurrency exceptions for `WaitForCall` when update	WaitForCall.cs	3
* Should I create new scope when initialize function instance??	ResumableFunction-Wait Functions.cs	46
* Refine `GetMethodsInfo` query	ResumableFunctions.Handler	UiService.cs	161

# Eid Vaction Wok on (ordered by priority)
* Hot Todos
* Write unit tests
* Finalize UI v1
* Video Records & Documentation
* Background Cleaning Job
* Publisher project