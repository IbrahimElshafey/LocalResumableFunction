# UI project
* Services View
	* Add Scan Status To Services View
* Wait Status InError not shown in views
* Add service Id fliter to
	* Resumable Functions
	* Method Groups
	* Pushed Calls
	* Logs
* Drop down with search for URNs in:
	* Pushed Calls view
	* Method Groups view
	* Resumable Functions 
* In Method Groups add link for pushed call for each group
* Tables problem on small screens
	


# UI Actions [Will be in UI v2]
* Wait templates for method group
* Actions on Wait 
	* Cancel (If Waiting)
	* Replay Go To (If Completed/Canceled)
	* Replay Go Before (If Completed/Canceled)
	* Replay Go After (If Completed/Canceled)
	* Set Matched (If Waiting)
* Actions on service
	* Find dead methods
	* Verify start waits exist in db for each RF
	* Instance in progress but not wait anything check
	* Validate URN duplication when scan if different method signature
	* Wait methods in same method group must have the same signature
	* Return failed instancs
	* Stop resumable function creation of new instances