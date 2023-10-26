# Todo
* IF closure changed it must propgate:
	* In method `WaitsRepo.CancelWait` > solve all cancel problems
	* In method `WaitProcessor.ExecuteAfterMatchAction`
* We SetClosure in places
	* Keep that there may be a looping
	* MatchIf Expression set [No Update WaitState NewInMemory]
	* Validate Method when requested for => AfterMatch,WhenCancel,Group.MatchIf [No Update WaitState NewInMemory]
	* Replay Wait New Template [No Update WaitState NewInMemory]
	* CallMethodByName after  => AfterMatch,WhenCancel,Group.MatchIf [UPDATE]
* You should not update/mutate state in this method in Group.MatchIf
* Add scope continuation tests for:
	- Global Scope
	- Local Closure scope
	- In sequance
	- In group
	- In function
	- Within After Match Call [May update local or global state]
	- Within Cancel Call [May update local or global state]
	- Within Group match filter [May update local or global state]

* Review the need for path property 
	* Used in CloneFirstWait

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


AddWait closure
	If root set closure from it's child
UpdateWait closure
	If not root find root and update all child closures