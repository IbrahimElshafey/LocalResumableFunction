# Closure Bug
* Closure occured when callback or match expression use local variable:
	* WaitsGroup.MatchIf (use immutable version of closure)//you can update the closure here

* When evaluate match we use immutable version of the closure
* When we resume the execution we use the mutable version of closure 
	* Mutable version may be shared for many waits under same method call





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
