using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumableFunctions.Handler.InOuts;

public abstract class ObjectWithLog
{
    [JsonIgnore]
    public int ErrorCounter { get; internal set; }

    [JsonIgnore]
    [NotMapped]
    public List<LogRecord> Logs { get; } = new();

    [JsonIgnore]
    public bool HasError => Logs.Any(x => x.Type == LogType.Error);

    public virtual void AddLog(string message, LogType logType = LogType.Info, string code = "")
    {
        var logRecord = new LogRecord
        {
            EntityType = GetType().Name,
            Type = logType,
            Message = message,
            Code = code,
            Created = DateTime.Now,
        };
        Logs.Add(logRecord);
        Console.WriteLine(logRecord);
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
        Logs.Add(logRecord);
        ErrorCounter++;
        if (ex != null)
        {
            logRecord.Message += $"\n{ex.Message}";
            logRecord.Message += $"\n{ex.StackTrace}";
        }
        Console.WriteLine(logRecord);
    }
}


