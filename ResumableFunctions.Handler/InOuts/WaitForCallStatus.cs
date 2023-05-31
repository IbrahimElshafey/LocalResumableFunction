namespace ResumableFunctions.Handler.InOuts;

public enum WaitForCallStatus
{
    ExpectedMatch,
    PartiallyMatched,
    Matched,
    NotMatched,
    ProcessingSucceed,
    ProcessingFailed,
    DuplicationCanceled
}
