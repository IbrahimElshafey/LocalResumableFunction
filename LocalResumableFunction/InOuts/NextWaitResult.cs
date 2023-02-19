namespace LocalResumableFunction.InOuts;

public class NextWaitResult
{
    public NextWaitResult(Wait result, bool isFinalEnd, bool isSubFunctionEnd)
    {
        Result = result;
        IsFinalExit = isFinalEnd;
        IsSubFunctionExit = isSubFunctionEnd;
    }

    public Wait Result { get; }
    public bool IsFinalExit { get; }
    public bool IsSubFunctionExit { get; }
}