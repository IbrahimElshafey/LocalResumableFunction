# Validate attributes usage
* [PushCallAttribute] must applied to method you want to wait
* [PushCallAttribute] 
	* muthod must have one input that is serializable
	* MethodUrn must not be null if attribute applied

# Validate Wait Requests
* Props used in Match and SetData expressions must be public and have getter and setter
* Local variables in match and set data expressions is not allowed
* You didn't set the `MatchExpression` for wait
* You didn't set the `MatchExpression` for first wait
* You didn't set the `SetDataExpression` for wait
* When the replay type is [GoToWithNewMatch],the wait to replay  must be of type [MethodWait]
* Go to the first wait with same match will create new separate function instance
* Go before the first wait with same match will create new separate function instance.
* Replay Go Before found no waits!!
* The wait named [{Name}] is duplicated in function body
