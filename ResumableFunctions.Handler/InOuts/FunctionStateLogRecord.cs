
namespace ResumableFunctions.Handler.InOuts;

public class LogRecord : IEntity
{
    public int Id { get; internal set; }
    public int? EntityId { get; internal set; }
    public string EntityType { get; internal set; }
    public LogType Type { get; internal set; } = LogType.Info;
    public string Message { get; internal set; }
    public DateTime Created { get; internal set; }
    public string Code { get; internal set; }

    public override string ToString()
    {
        return $"{Type}: {Message}";
    }
}
