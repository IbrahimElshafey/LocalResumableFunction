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
	* Raised in same service
	* Raised in same service another instance when using load balancer
* Diffrent services may update/add same methods wait group at same time


# Reading
* lock in async method https://blog.cdemi.io/async-waiting-inside-c-sharp-locks/