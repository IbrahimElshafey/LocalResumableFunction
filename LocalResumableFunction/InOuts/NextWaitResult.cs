namespace LocalResumableFunction.InOuts
{
    public class NextWaitResult
    {
        public NextWaitResult(Wait result, bool isFinalEnd, bool isSubFunctionEnd)
        {
            Result = result;
            IsFinalExit = isFinalEnd;
            IsSubFunctionExit = isSubFunctionEnd;
        }
        public Wait Result { get; private set; }
        public bool IsFinalExit { get; private set; }
        public bool IsSubFunctionExit { get; private set; }
    }
}