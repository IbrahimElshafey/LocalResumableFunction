# Validate attributes usage
* [PushCallAttribute] must applied to method you want to wait
* [PushCallAttribute] 
	* muthod must have one input that is serializable
	* MethodUrn must not be null if attribute applied
* Validate input output type serialization
* Function URN name must not duplicate
* Method URN name must not duplicate in same class

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
* Not allowed to create local varaibles in methods that have no resumable function attribute
``` C#
private MethodWait<OwnerApproveClientInput, OwnerApproveClientResult> WaitOwnerApproveClient()
{
    int y = 11;//Not allowed to create local varaibles in methods that have no resumable function attribute
    return Wait<OwnerApproveClientInput, OwnerApproveClientResult>(_service.OwnerApproveClient, "Wait Owner Approve Client")
                    .MatchIf((approveClientInput, approveResult) => approveClientInput.TaskId == OwnerTaskId.Id && y == 11)
                    .AfterMatch((approveClientInput, approveResult) =>
                    {
                        OwnerTaskResult = approveResult;
                        OwnerApprovalInput = approveClientInput;
                    });
}
```
