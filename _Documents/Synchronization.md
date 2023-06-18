# Synchronization Scenarios
* Two waits trying update same FunctionState [done]
	* Using distributed lock for Update Function Data method
	* Optimistic concurrency token will be validated and exception will be throw and hangfire will replay the task
* First wait closed but new request come before create new one [done]
	* Dont update the first wait , clone it 
* Update pushed calls counter [Done]
	* Seprate table and one record for each match
* Database.EnsureCreated(); in same time from multiple services [Done]
	* We should use inter services lock
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

