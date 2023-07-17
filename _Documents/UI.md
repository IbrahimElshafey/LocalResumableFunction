# UI project
* Local method must be registered many
* Set params in Hash and enable reload

* Date range filter for:
	* Logs view
	* Pushed Calls view
* Infinite scroll for:
	* Logs view
	* Pushed Calls view

* Wait Status InError not shown in views
* Tabels problem on small screens
	


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