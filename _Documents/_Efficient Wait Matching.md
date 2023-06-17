# When process pushed call received for service (Each service process it's waits)
* One match per group
* Time wait clone if first
* Review logs to service , why first error only
* Optimize wait table indexes to enable fast wait insertion
	* Remove index 
		* [ParentWaitId] in [dbo].[Waits]
* Review check after locks
* Review activate and deactivate function