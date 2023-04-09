# Simple Stupid Locking
* Table with no index and Insert/Delete only (No Update)
* Indexed string column with fixed length,Creation date column
* Process that may cause lock add row at starting contains (EntityName-EntityID)
* Process check if row exist the entity is locked
* If no row exist then process can start
* After process finished the row will be deleted
* Background process to delete dead locks

# Scenarios
* Two waits matched for same FunctionState
* First wait closed but new request come before create new one
* Update pushed methods calls counter
* Database.EnsureCreated(); in same time
* Multiple scan process in same time
	* Raised in same service [done]
	* Raised in same service another instance when using load balancer
* Diffrent services may update/add same methods wait group at same time


# Reading
* lock in async method https://blog.cdemi.io/async-waiting-inside-c-sharp-locks/


# Tables
* FunctionStates (Insert,Update,Delete)
* MethodIdentifiers (Insert,Update)
* ServicesData (Insert,Update)

* Waits (Insert)


# Read
* EF core Handling Concurrency Conflicts
	https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=fluent-api#native-database-generated-concurrency-tokens
* Item-level locks for a large number of items
	https://codereview.stackexchange.com/questions/105523/item-level-locks-for-a-large-number-of-items


1 - For methods in the same service
2 - For methods in different services
3 - Database locks
ImmutableList<T> Class
ConcurrentBag<T> Class

https://stackoverflow.com/questions/45943048/ef-core-fluent-api-set-all-column-types-of-interface
https://stackoverflow.com/questions/51763168/common-configurations-for-entities-implementing-an-interface
