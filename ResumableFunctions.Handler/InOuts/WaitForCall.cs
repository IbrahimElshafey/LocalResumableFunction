namespace ResumableFunctions.Handler.InOuts;

//todo:change this to be imutable entity with no update
public class WaitForCall : IEntity, IEntityInService
{
    public int Id { get; internal set; }
    public PushedCall PushedCall { get; internal set; }
    public int PushedCallId { get; internal set; }
    public int WaitId { get; internal set; }
    public int? ServiceId { get; set; }
    public int FunctionId { get; internal set; }
    public int StateId { get; internal set; }
    public WaitForCallStatus Status { get; internal set; }
    public DateTime Created { get; internal set; }

}
