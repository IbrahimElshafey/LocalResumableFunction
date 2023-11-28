using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction;

public interface ILogsRepo
{
    Task AddErrorLog(Exception ex, string errorMsg, int statusCode);
    Task AddLog(string msg, LogType logType, int statusCode);
    Task AddLogs(LogType logType, int statusCode, params string[] msgs);
}
