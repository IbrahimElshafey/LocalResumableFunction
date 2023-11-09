# Todo
* Validate wait name duplication
* Validate go back to closure update
	* Re-evaluate match expression
	* Use same old match expression
* Closure may be from normall method and continuation in replay go back may reuse same old private method data?? 
* UI service must not use EF context directly
* Replay time wait not same description as old wait
* UI when click errors link show log errors not all
* Cleaning closures when delete old state `CleanCompletedFunctionInstances`



* Analyzer
* Review CanPublishFromExternal and IsLocalOnly
	* Should it defined for method group or method idenetifier
* Review SaveChanges call
* Enable change testshell settings
	* Run unit tests in parallel

* Use same dll in two different services must be allowed

* Message Pack private setter props serialization
* Parameter check lib use
* Use RequestedByFunctionId prop in TimeWaitInput to refine match for time waits
* Confirm One Transaction per bussiness unit
* Speed tests => work in memory
* How I handle call recieving while creating db

* Publish from client to multiple services
	* Pushed call must have flag fields for:
		* Is from external
		* From service
		* To Service
		* Processing behavior (Process Locally, or propagate in cluster)
	* When more than one services share the db
		* Should external call hits all services? No
	* Should we define an external id for pushed call?


# Later
* How to check that pushed call processed by all affected services?
	* How to check that pushed call can be deleted?
* Publisher Project Todos
* Roslyn_Analyzer
* What if type is not serializable??