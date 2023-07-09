namespace ResumableFunctions.Handler.InOuts;

public enum MatchStatus
{
    ExpectedMatch = 0,
    Matched = 1,
    NotMatched = -1,
    PartiallyMatched
}

public enum InstanceUpdateStatus
{
    NotUpdatedYet = 0,
    UpdateFailed = -1,
    UpdateSuccessed = 1,
}


public enum ExecutionStatus
{
    NotStartedYet = 0,
    ExecutionSuccessed = 1,
    ExecutionFailed = -1,
}
