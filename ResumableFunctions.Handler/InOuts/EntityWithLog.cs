using System.ComponentModel.DataAnnotations.Schema;

namespace ResumableFunctions.Handler.InOuts;

public class EntityWithLog : IEntity
{
    public int Id { get; internal set; }
    public DateTime Created { get; internal set; }


    [NotMapped]
    public List<LogRecord> Logs { get; } = new();

    public void AddLog(string message, LogType logType = LogType.Info, string code = "")
    {
        Logs.Add(new LogRecord
        {
            EntityType = GetType().Name,
            Type = logType,
            Message = message,
            Code = code
        });
    }
    public void AddError(string message, Exception ex = null, string code = "")
    {
        var logRecord = new LogRecord
        {
            EntityType = GetType().Name,
            Type = LogType.Error,
            Message = message,
            Code = code
        };
        Logs.Add(logRecord);
        if(ex != null )
        {
            logRecord.Message += $"\n{ex.Message}";
            logRecord.Message += $"\n{ex.StackTrace}";
        }
    }
}

