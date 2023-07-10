namespace ResumableFunctions.Handler.InOuts;

public static class ObjectWithLogBehavior
{
    public static bool HasErrors(this IObjectWithLog _this) => _this.Logs.Any(x => x.Type == LogType.Error);

    internal static void AddLog(this IObjectWithLog _this, string message, LogType logType, int code)
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
    internal static void AddError(this IObjectWithLog _this, string message, int code, Exception ex = null)
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
            logRecord.Message += $"\n{ex}";
        }
        //_logger.LogError(message, logRecord, ex);
    }
}