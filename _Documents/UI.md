# UI Todo
* UI service must not use EF context directly
* UI when click errors link show log errors not all

* Filtering controls at top for:
	* Function Instances
	* Waits in method group
* Infinite scroll for:
	* Logs view
	* Pushed Calls view
	* Waits in method group
	* Function Instances
* Tabels problem on small screens
* Restrict access to UI from servers only



# UI V2
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
* Date range filter for:
	* Logs view
	* Pushed Calls view
* Localization For UI and log messages