# Todo
* Wait processing review
	* Wait creation time and pushed call time must be taken in consideration when get pending waits

* Provide an advanced API for tests that enable complex queries for DB

* Suspend Hangfire old jobs

* Review concurrency with [computer and paper]
	* Review function state update lock `ExecuteAfterMatchAction`

* Wait template duplication review
	* Must be linked to method id and group id
* Time waits review
* When instance completed update sub tree status
* Complex local var in closure
	* What if type is not serializable??

* Cleaning closures when delete old state `CleanCompletedFunctionInstances` method
* Reset Errors after function complete background query

* Use same dll in two different services must be allowed

* Parameter check lib use
* Confirm One Transaction per bussiness unit
	* Review SaveChanges call

* How to check that pushed call processed by all affected services?
	* How to check that pushed call can be deleted?