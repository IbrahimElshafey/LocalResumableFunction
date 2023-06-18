namespace ResumableFunctions.Handler.InOuts;

public enum MatchStatus
{
    ExpectedMatch,
    Matched,
    NotMatched,
    PartiallyMatched,
    DuplicationCanceled
}

public enum InstanceUpdateStatus
{
    NotUpdatedYet,
    UpdateFailed,
    UpdateSuccessed,
}


public enum ExecutionStatus
{
    NotStartedYet,
    Successed,
    Failed,
}
