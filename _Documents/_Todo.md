# Todo
* Add scope UPDATE continuation tests for:
	- Global Scope
	- Local Closure scope
	- In sequance
	- In group
	- In function
	- Within After Match Call [May update local or global state]
	- Within Cancel Call [May update local or global state]
	- Within Group match filter [May update local or global state]
* Analyzer
* Review CanPublishFromExternal and IsLocalOnly
	* Should it defined for method group or method idenetifier
* Review SaveChanges call
* Enable change testshell settings
	* Run unit tests in parallel if not localDB
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

