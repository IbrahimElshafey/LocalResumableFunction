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

# Synchronization Cross Services on different servers
* Table with Insert/Delete only (No Update)
	* Indexed string column for entity name
	* Intger column for entity ID
	* Creation date column
* Process that may cause lock add row at starting contains (EntityName-EntityID)
* Process check if row exist the entity is locked
* If no row exist then process can start
* After process finished the row will be deleted
* Background process to delete dead locks
* Can I use https://github.com/madelson/DistributedLock

# Synchronization cross processes on same servers or same process tasks
* Overview of synchronization primitives 
	* https://learn.microsoft.com/en-us/dotnet/standard/threading/overview-of-synchronization-primitives
* SemaphoreSlim is a lightweight alternative to Semaphore and can be used only for synchronization within a single process boundary.
* On Windows, you can use Semaphore for the inter-process synchronization. 
* Named Mutexes can be used in Window ,Linux ,and Mac.

# Read
* EF core Handling Concurrency Conflicts 		
	* https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=fluent-api#native-database-generated-concurrency-tokens
* Item-level locks for a large number of items
	* https://codereview.stackexchange.com/questions/105523/item-level-locks-for-a-large-number-of-items

* What is System.Collections.Concurrent Namespace
	* ImmutableList<T> Class
	* ConcurrentBag<T> Class

* Confuigure Interfaces In EF
https://stackoverflow.com/questions/45943048/ef-core-fluent-api-set-all-column-types-of-interface
https://stackoverflow.com/questions/51763168/common-configurations-for-entities-implementing-an-interface
