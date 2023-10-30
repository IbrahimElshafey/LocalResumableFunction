# Closure Bug
* Closure must be immutable so match expression can be evaluated correctly
* Closure must be mutable so callbacks can modify it
* We should create two version of closure
	* immutable: that is for method waits only
	* mutable: that is one shared closure for all waits in specific wait point that share same parent

* Closure occured when callback or match expression use local variable:
	* MethodWait.MatchIf (use immutable version of closure)
	* WaitsGroup.MatchIf (use immutable version of closure)
	* MethodWait.AfterMatch (use mutable version of closure)
	* MethodWait.WhenCancel (use mutable version of closure)
* When evaluate match we use immutable version of closure
* When we resume the execution we use immutable version of closure
* NodeWait => Is the wait that is parent to all waits in specefic stop point
	* His parent is null or parent have a differnt stop point or function
* RootWait => Is node wait that has no parent
Requested by function, Stop point = state after wait

* All childerns under same NODE wait must share same locals and closure
* IF closure changed it must propgate:
	* In method `WaitsRepo.CancelWait` > solve all cancel problems
	* In method `WaitProcessor.ExecuteAfterMatchAction`
* We SetClosure in places
	* Keep in mind that may be a looping exist [deep clone closure]
	* MatchIf Expression set [No Update WaitState NewInMemory]
	* Validate Method when requested for => AfterMatch,WhenCancel,Group.MatchIf [No Update WaitState NewInMemory]
	* Replay Wait New Template [No Update WaitState NewInMemory]
	* CallMethodByName after  => AfterMatch,WhenCancel,Group.MatchIf [UPDATE]
* You should not update/mutate state in this method in Group.MatchIf

* We need to detect if object is modified or not?
Is closure or function state chnaged after
- MatchAction
- Cancel Action
