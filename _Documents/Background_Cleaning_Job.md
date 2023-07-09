# Background Cleaning Job
* Delete soft deleted rows
* Delete completed function instance from DB
	* It's logs
	* Waits
* Deleted completed pushed call and it's records
* Delete old logs for scan sessions


* Delete unused WaitTemplates
	* Wait templates that has no active waits
* Delete unused Method Identifires 
	* Not exist in code

# Tables that must be cleanded
* [dbo].[FunctionStates]
* [dbo].[Logs]
* [dbo].[PushedCalls]
* [dbo].[Waits]
* [dbo].[WaitsForCalls]