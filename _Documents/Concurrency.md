# Concurrency Scenarios
* Review unit of work blocks
* Review concurrency with [pen and paper]
	* Review function state update lock `ExecuteAfterMatchAction`
* Update [dbo].[WaitProcessingRecord] row
* [dbo].[MethodIdentifiers] is updatable
* Different services may try to add same MethodGroup at same time 
	* Uniqe index exception handel
* Review all places where database update occurs

# Find lock postions
* BackgroundJobExecutor.ExecuteWithLock