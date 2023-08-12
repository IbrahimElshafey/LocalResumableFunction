# What is New?
* Save local variables defined in the resumable function body or other methods called by it.
* Add `OnErrorOccurred` in `ResumableFunctionsContainer` to be overridden for notification about errors.
* Add `WhenCancel` to `MethodWait` to pass a callback when the wait canceled.
* Fix exposing not necessary API to the end user.
* Enahnce mandatory part extraction of the match expressions.
* AfterMatch,WaitsGroup.MatchIf, and WhenCancel now accept a methods callback as input, not an expression tree.