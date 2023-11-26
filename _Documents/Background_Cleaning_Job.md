# Tables that must be cleanded 
* Delete unused private data rows when delete completed jobs
* Delete unused Method Identifiers that do not exist in the code anymore
	* How to know if method is not used and safe to be deleted?
	* Method may be a 
		* resumable function or sub resumable function -> safe to delete if no waits requested by it
		* pushed calls -> safe to delete if no waits requested by it
* How to check that pushed call processed by all affected services so it can be deleted?
