# Concurrency Scenarios
* Update [dbo].[WaitProcessingRecord] row
* [dbo].[MethodIdentifiers] is updatable
* Different services may try to add same MethodGroup at same time 
	* Uniqe index exception handel
* Review all places where database update occurs