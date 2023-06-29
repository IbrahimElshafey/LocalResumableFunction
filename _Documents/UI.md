# UI project
* Services View
	* Add pushed call count column
	* Links should work and filter views data
	* Add Errors Overview View
* Resumable Functions
	* Filters
* Method Group 
	* Filters
	* Methods in group view
	* Waits for method group view
* Pushed Calls
	* Filter
* All logs View
	* Filtering



* Waits tree
	* Extra Data
	


# UI Actions [Will be in UI v2]
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