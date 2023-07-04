# Background Cleaning Job
* Delete soft deleted rows
* Delete completed function instance from DB
	* It's logs
	* Waits
* Deleted completed pushed call and it's waits for call records
* Delete old logs for scan sessions


* Delete unused WaitTemplates
* Delete unused Method Identifires that not exist in code

# Tables that must be cleanded
* [dbo].[FunctionStates]
* [dbo].[Logs]
* [dbo].[PushedCalls]
* [dbo].[Waits]
* [dbo].[WaitsForCalls]