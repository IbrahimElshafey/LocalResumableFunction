namespace ResumableFunctions.Handler.InOuts;

public enum ReplayType
{
    /// <summary>
    /// Execute code after wait `x` and get next wait [ResumeExecution] `y` or exit if no wait
    /// </summary>
    GoAfter,//todo:can be replaced with goto statement

    /// <summary>
    /// Execute code before wait `x` and then re-wait `x` again.
    /// </summary>
    GoBefore,//todo:can be replaced with goto statement

    /// <summary>
    /// Execute code before wait `x` and then re-wait `x` again but with new match expression.
    /// </summary>
    GoBeforeWithNewMatch,

    /// <summary>
    /// Re-wait `x` again and cancel other sibling waits in current function.
    /// </summary>
    GoTo,//todo:can be replaced with goto statement
    GoToWithNewMatch,
}