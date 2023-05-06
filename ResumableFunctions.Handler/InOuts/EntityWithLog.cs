using System.ComponentModel.DataAnnotations.Schema;

namespace ResumableFunctions.Handler.InOuts;

public class EntityWithLog : IEntity
{
    public int Id { get; internal set; }
    public DateTime Created { get; internal set; }


    [NotMapped]
    public List<LogRecord> Logs { get; private set; } = new List<LogRecord>();

    public void AddLog(LogType logType, string message, string code = "")
    {
        Logs.Add(new LogRecord
        {
            EntityType = GetType().Name,
            Type = logType,
            Message = message,
            Code = code
        });
    }
}

