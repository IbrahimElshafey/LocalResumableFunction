# Short and Mid Todos
* Use same dll in two different services must be allowed
* Remove SaveChangesAsync and,SaveChangesdDirectly from repo implementations
	* Use transactions
* Dynamic loaded dll
* Build Nuget package to target multiple runtimes and versions
* Unify logging and make separate DB for logs

* If found RF database with schema diffrence use new one and update appsettings

* Provide an advanced API for tests that enable complex queries for DB

* Cancel old scan jobs when rescan
	* Self cancel if job creation date less that last scan session date

* Parameter check lib use
* Confirm One Transaction per bussiness unit
	* Review SaveChanges call

* How to check that pushed call processed by all affected services so it can be deleted?

* Composite primary key for log record (id,entity_id,entity_type)


# Big Todos
* Reliable communication between services with each others and with clients
* Publisher Project TODOs
* Side by Side Versions
* Analyzer