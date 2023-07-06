
namespace ResumableFunctions.Handler.InOuts;

public class LogRecord : IEntity
{
    public int Id { get; internal set; }
    public int? EntityId { get; internal set; }
    public string EntityType { get; internal set; }
    public LogType Type { get; internal set; } = LogType.Info;
    public string Message { get; internal set; }
    public DateTime Created { get; internal set; }
    public int Code { get; internal set; }
    public int? ServiceId { get; set; }

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
