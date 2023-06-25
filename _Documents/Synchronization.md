# Synchronization Scenarios
* Push call at same time
* Update [dbo].[WaitsForCalls] row
* [dbo].[MethodIdentifiers] is updatable
* Multiple scan process in same time [Done]
	* Using distributed lock $"Scanner_StartServiceScanning_{_currentServiceName}"
* Different services may try to add same MethodGroup at same time 
	* Uniqe index exception handel

# Synchronization cross processes on same servers or same process tasks
* Overview of synchronization primitives 
	* https://learn.microsoft.com/en-us/dotnet/standard/threading/overview-of-synchronization-primitives
* SemaphoreSlim is a lightweight alternative to Semaphore and can be used only for synchronization within a single process boundary.
* On Windows, you can use Semaphore for the inter-process synchronization. 
* Named Mutexes can be used in Window ,Linux ,and Mac.

