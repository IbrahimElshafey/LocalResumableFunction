# UI project
* Add service Id fliter to
	* Resumable Functions
	* Method Groups
	* Pushed Calls
* Drop down with search for URNs in:
	* Pushed Calls view
	* Method Groups view
	* Resumable Functions 
* Date range filter for:
	* Logs view
	* Pushed Calls view
* Infinite scroll for:
	* Logs view
	* Pushed Calls view
* In Method Groups add link for pushed call for each group
* Wait Status InError not shown in views
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