# Refactor
* Refactor long methods
* Optimize wait table indexes to enable fast wait insertion
	* Remove index 
		* [ParentWaitId] in [dbo].[Waits]