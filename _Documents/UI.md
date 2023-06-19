# Hot todos
* Pushed call view
* Instance details view
* Method waits view
# UI project
* Errors Overview Page
* RF Instance Details
	* Waits tree
		* Wait Name
		* Status
		* Is Replay
		* Extra Data
		* Wait Type
		* Created
		* If match --> Pushed method input & output
		* Expressions if method wait
		* Remove Need Load Function Data for match
		* Count Expression if group
		* Actions on Wait 
			* Cancel (If Waiting)
			* Wait Again (If Completed/Canceled)
			* Wait Again and Execute Code before (If Completed/Canceled)
			* Wait Next Wait (If Completed/Canceled)
			* Set Matched (If Waiting)
* All Logs View
* Actions on service
	* Find dead methods
	* verify method signatures
	* verify start waits exist in db for each RF
	* Verify Scanned Methods 
	* Instance in progress but not wait anything check
	* Validate URN duplication when scan if diffrent method signature
	* Wait methods in same method group must have the same signature
	* Return failed instancs
	* Stop resumable function new instances