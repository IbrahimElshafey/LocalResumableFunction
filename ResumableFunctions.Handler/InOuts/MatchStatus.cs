namespace ResumableFunctions.Handler.InOuts;

public enum MatchStatus
{
    ExpectedMatch = 0,
    Matched = 1,
    NotMatched = -1,
}


public enum ExecutionStatus
{
    NotStartedYet = 0,
    ExecutionSucceeded = 1,
    ExecutionFailed = -1,
}
