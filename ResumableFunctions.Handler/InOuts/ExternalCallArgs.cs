namespace ResumableFunctions.Handler.InOuts;

public class TimeWaitInput
{
    //public int ServiceId { get; set; }
    //public int GroupId { get; set; }
    public string TimeMatchId { get; set; }

    public int? FakeProp { get; set; }
    //public int FunctionId { get; set; }
    //public int MethodId { get; set; }
}

public class ExternalCallArgs
{
    public string ServiceName { get; set; }
    public MethodData MethodData { get; set; }
    public object Input { get; set; }
    public object Output { get; set; }
}