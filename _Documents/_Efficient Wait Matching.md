# When process pushed call received for service (Each service process it's waits)
* Use Wait for call in WaitProcessor
* Review logs to service , why first error only
* One match per group
* Optimize wait table indexes to enable fast wait insertion
	* Remove index 
		* [ParentWaitId] in [dbo].[Waits]
* Review check after locks