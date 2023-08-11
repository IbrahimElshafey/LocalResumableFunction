namespace ResumableFunctions.Handler.InOuts.Entities;

public class LogRecord : IEntity<long>
{
    public long Id { get; set; }
    public long? EntityId { get; set; }
    public string EntityType { get; set; }
    public LogType Type { get; set; } = LogType.Info;
    public string Message { get; set; }
    public DateTime Created { get; set; }
    public int StatusCode { get; set; }
    public int? ServiceId { get; set; }

    public override string ToString()
    {
        return $"Type: {Type},\n" +
               $"Message: {Message}\n" +
               $"EntityType: {EntityType}\n" +
               $"EntityId: {EntityId}\n" +
               $"Code: {StatusCode}\n"
               ;
    }
    public (string Class, string Title) TypeClass()
    {
        switch (Type)
        {
            case LogType.Info: return ("w3-pale-blue", "Info");
            case LogType.Error: return ("w3-deep-orange", "Error");
            case LogType.Warning: return ("w3-amber", "Warning");
        }
        return ("w3-gray", "Undefined");
    }
}
