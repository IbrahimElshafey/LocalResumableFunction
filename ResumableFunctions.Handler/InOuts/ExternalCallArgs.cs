namespace ResumableFunctions.Handler.InOuts;

public class TimeWaitInput
{
    public string TimeMatchId { get; set; }
}

public class ExternalCallArgs
{
    public string ServiceName { get; set; }
    public MethodData MethodData { get; set; }
    public object Input { get; set; }
    public object Output { get; set; }
}