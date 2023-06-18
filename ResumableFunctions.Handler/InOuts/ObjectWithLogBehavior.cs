namespace ResumableFunctions.Handler.InOuts;

//mixin

public static class ObjectWithLogBehavior
{
    public static bool HasErrors(this IObjectWithLog _this) => _this.Logs.Any(x => x.Type == LogType.Error);

    public static void AddLog(this IObjectWithLog _this, string message, LogType logType = LogType.Info, string code = "")
    {
        var logRecord = new LogRecord
        {
            EntityType = _this.GetType().Name,
            Type = logType,
            Message = message,
            Code = code,
            Created = DateTime.Now,
        };
        _this.Logs.Add(logRecord);
        //_logger.LogInformation(message, logRecord);
    }
    public static void AddError(this IObjectWithLog _this, string message, Exception ex = null, string code = "")
    {
        var logRecord = new LogRecord
        {
            EntityType = _this.GetType().Name,
            Type = LogType.Error,
            Message = message,
            Code = code,
            Created = DateTime.Now,
        };
        _this.Logs.Add(logRecord);
        _this.ErrorCounter++;
        if (ex != null)
        {
            logRecord.Message += $"\n{ex.Message}";
            logRecord.Message += $"\n{ex.StackTrace}";
        }
        //_logger.LogError(message, logRecord, ex);
    }
}


