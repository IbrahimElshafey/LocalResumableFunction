# What is New 1.0.10?
* Save local variables defined in the resumable function body or other methods called by it.
* Add `OnErrorOccurred` in `ResumableFunctionsContainer` to be overridden for notification about errors.
* Add `WhenCancel` to `MethodWait` to pass a callback when the wait canceled.
* Fix exposing not necessary API to the end user.
* Enahnce mandatory part extraction of the match expressions.
* AfterMatch,WaitsGroup.MatchIf, and WhenCancel now accept a methods callback as input, not an expression tree.

# What's New V2?
* GoBack call deleted you can use goto C# keyword instead
* Enable updating closure (local variables) in AfterMatch,Cancel and,GroupMatch callbacks.
* Enable passing parameters for sub resumable functions.
* Attributes validation to not allow multiple attributes
* Save loclas and closures in separate table
* In publisher project
	* Add `OnDiskFailedRequestRepo` to save failed requests to disk.
	* Publish call to many services.
* Fix some bugs
# PDF for Resumable Functions
https://pdfhost.io/edit?doc=4c5103c7-39b6-4eaf-85ab-914a08183079
https://pdfhost.io/v/DFgolpDyq_Resumable_Functions