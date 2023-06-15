namespace ResumableFunctions.Handler.InOuts;

public record WaitId(int Id,int FunctionId,int StateId)
{
    public bool FullMatch { get; set; }
}
