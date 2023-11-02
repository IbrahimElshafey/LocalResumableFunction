# Closure Bug
* All waits requested by
	* Same CallerName
	* Under same RequestedByFunctionId parent
* When evaluate match we use immutable version of closure [So it will be moved to MethodWait class]
* When we resume the execution we use the mutable version of closure 
	* [Mutable version is shared for waits under same method call]

* We should create two version of closure
	* immutable: that is a one to one with wait
	* mutable: that is one shared closure for all/many waits in specific stop point

* Closure occured when callback or match expression use local variable:
	* MethodWait.MatchIf (use immutable version of closure)
	* WaitsGroup.MatchIf (use immutable version of closure)
	* MethodWait.AfterMatch (use mutable version of closure)
	* MethodWait.WhenCancel (use mutable version of closure)

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


- Multiple replay and local variables

* What if closure is not serializable
