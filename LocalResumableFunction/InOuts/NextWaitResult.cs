namespace LocalResumableFunction.InOuts;

public class NextWaitResult
{
    public NextWaitResult(Wait result, bool isFinalEnd, bool isSubFunctionEnd)
    {
        Result = result;
        FinalExit = isFinalEnd;
        SubFunctionExit = isSubFunctionEnd;
    }

    public Wait Result { get; }
    private bool FinalExit { get; }
    private bool SubFunctionExit { get; }

    public bool IsFinalExit() => Result is null && FinalExit;

    public bool IsSubFunctionExit() => Result is null && SubFunctionExit;
}