
namespace ResumableFunctions.Handler.InOuts;

public class LockRecord : IEntity
{
    public int Id { get; internal set; }

    public DateTime Created { get; internal set; }
    public string EntityName { get; internal set; }
    public int EntityId { get; internal set; }
}
