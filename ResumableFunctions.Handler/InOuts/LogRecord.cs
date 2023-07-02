
namespace ResumableFunctions.Handler.InOuts;

public class LogRecord : IEntity
{
    public long Id { get; internal set; }
    public long? EntityId { get; internal set; }
    public string EntityType { get; internal set; }
    public LogType Type { get; internal set; } = LogType.Info;
    public string Message { get; internal set; }
    public bool IsCustom { get; internal set; }
    public DateTime Created { get; internal set; }
    public string Code { get; internal set; }
    public long? ServiceId { get; set; }

    public override string ToString()
    {
        return $"Type: {Type},\n" +
               $"Message: {Message}\n" +
               $"EntityType: {EntityType}\n" +
               $"EntityId: {EntityId}\n" +
               $"Code: {Code}\n"
               ;
    }

    public (string Class, string Title) TypeClass()
    {
        switch (Type)
        {
            case LogType.Info: return ("w3-gray", "Info");
            case LogType.Error: return ("w3-deep-orange", "Error");
            case LogType.Warning: return ("w3-amber", "Warning");
        }
        return ("w3-gray", "Undefined");
    }
}
