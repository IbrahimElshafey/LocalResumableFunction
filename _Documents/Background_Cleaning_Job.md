# Background Cleaning Job
* Delete completed/cancled function instance from DB
	* It's logs
	* Waits
* Deleted completed pushed call and it's waits for call records
* Delete old logs for scan sessions
* Delete soft deleted rows
* Delete unused WaitTemplates
* Delete unused Method Identifires that not exist in code