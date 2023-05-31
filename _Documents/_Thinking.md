
# Optimize wait matching
* Query to SQL
	* https://learn.microsoft.com/en-us/dotnet/api/system.data.linq.datacontext.getcommand?view=netframework-4.8.1
* After wait requested 
* Translate match expression to SQL query
* Use NoSql databases to store states
* Can I use https://github.com/sebastienros/yessql to store states
* We can create table/database for each resumable function

## In current
* Use Instance ID match
* Use https://github.com/dadhi/FastExpressionCompiler to fast compile match and set data expression
* Fast serialization and deserialization for
	* StateObject https://github.com/rikimaru0345/Ceras
	* Wait ExtraData
	* Expressions
* String to query expression
	* https://github.com/zzzprojects/System.Linq.Dynamic.Core
* Fast Lightweight Expression Evaluator
	* https://github.com/mparlak/Flee
* Expression Serialization
	* https://github.com/reaqtive/reaqtor/tree/main/Nuqleon/Core/LINQ/Nuqleon.Linq.Expressions.Bonsai.Serialization
* Project that serialize expression trees
	* https://reaqtive.net/blog/2021/05/sequences-linq-rx-reaqtor-part-05-remotable-expressions
* Back to method compile ans save solution
	
	
	
	
	
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

https://stackoverflow.com/questions/45943048/ef-core-fluent-api-set-all-column-types-of-interface
https://stackoverflow.com/questions/51763168/common-configurations-for-entities-implementing-an-interface



# Aggregate Column Feature (will be separate project)
* Create table `AggregateDefinition` with columns
	* EntityName (sush as Orders)
	* AggregateName (such as FailedOrdersCount,TotalPayments)
	* AggregateFunction (such as SUM, COUNT, AVG, LAST,...) or user defined
	* ResetValue (such as -100 default null)
	* KeepValuesAfterAggregation (true or false)
* Create table `AggregateValues` with columns 'No update just insersion and delete'
	* AggregateDefinitionId
	* Number Value
	* CreationDate
	* IsAggregation (boolean)

## Example
* Define Aggregate `DefineAggregate(forTable: "Post",name: "LikesCount",aggregateFunction: "SUM")`
* Use when like button click `post.AddAggregateValue("LikesCount",1)`
* Use when unlike button clicked `post.AddAggregateValue("LikesCount",-1)`
* When user totally chnaged the content of the post `post.ResetAggregate("LikesCount")`
* When you wanty to display like counts `post.GetAggregate("LikesCount")`

# Table File Log
* This will be a separate test project to know more about reading/writing to files
* How database ACID work
