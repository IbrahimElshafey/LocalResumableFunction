# Todo
* Publish from client to multiple services
	* Pushed call must have flag fields for:
		* Is from external
		* From service
		* To Service
		* Processing behavior (Process Locally, or propagate in cluster)
	* When more than one services share the db
		* Should external call hits all services
	* Should we define an external id for pushed call?
* Review CanPublishFromExternal
* How I handle call recieving while creating db
* Add IFailedRequestHandler Faster implementation
	* Save failed requests to disk
* Review SaveChanges use
* Enable change testshell settings
	* Run unit tests in parallel if not localDB
* Use same dll in two different services must be allowed

* Message Pack private setter props serialization
* Parameter check lib use
* Use RequestedByFunctionId prop in TimeWaitInput to refine match for time waits
* Confirm One Transaction per bussiness unit
* Speed tests => work in memory
