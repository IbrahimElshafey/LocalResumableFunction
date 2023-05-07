using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace ResumableFunctions.Handler.InOuts;

public class EntityWithLog : IEntity
{
    [Key]
    public int Id { get; internal set; }
    public DateTime Created { get; internal set; }
    public int ErrorCounter { get; internal set; }


    [NotMapped]
    public List<LogRecord> Logs { get; } = new();

    public virtual void AddLog(string message, LogType logType = LogType.Info, string code = "")
    {
        Logs.Add(new LogRecord
        {
            EntityType = GetType().Name,
            Type = logType,
            Message = message,
            Code = code,
            Created = DateTime.Now,
        });
    }
    public virtual void AddError(string message, Exception ex = null, string code = "")
    {
        var logRecord = new LogRecord
        {
            EntityType = GetType().Name,
            Type = LogType.Error,
            Message = message,
            Code = code,
            Created = DateTime.Now,
        };
        Debug.WriteLine("Error: " + message);
        Logs.Add(logRecord);
        ErrorCounter++;
        if (ex != null)
        {
            logRecord.Message += $"\n{ex.Message}";
            logRecord.Message += $"\n{ex.StackTrace}";
        }
    }
}

