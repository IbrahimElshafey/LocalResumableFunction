using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess;

internal class LogsRepo : ILogsRepo
{
    private readonly WaitsDataContext _context;
    private readonly IResumableFunctionsSettings _settings;
    private readonly ILogger<ServiceRepo> _logger;

    public LogsRepo(
        WaitsDataContext context,
        IResumableFunctionsSettings settings,
        ILogger<ServiceRepo> logger)
    {
        _context = context;
        _settings = settings;
        _logger = logger;
    }
    public async Task AddErrorLog(Exception ex, string errorMsg, int statusCode)
    {
        _logger.LogError(ex, errorMsg);
        _context.Logs.Add(new LogRecord
        {
            EntityId = _settings.CurrentServiceId,
            EntityType = nameof(ServiceData),
            Message = $"{errorMsg}\n{ex}",
            Created = DateTime.UtcNow,
            ServiceId = _settings.CurrentServiceId,
            Type = LogType.Error,
            StatusCode = statusCode
        });
        await _context.SaveChangesdDirectly();
    }

    public async Task AddLog(string msg, LogType logType, int statusCode)
    {
        _context.Logs.Add(new LogRecord
        {
            EntityId = _settings.CurrentServiceId,
            EntityType = nameof(ServiceData),
            Message = msg,
            ServiceId = _settings.CurrentServiceId,
            Type = logType,
            Created = DateTime.UtcNow,
            StatusCode = statusCode
        });
        await _context.SaveChangesdDirectly();
    }

    public async Task AddLogs(LogType logType, int statusCode, params string[] msgs)
    {
        foreach (var msg in msgs)
        {
            _context.Logs.Add(new LogRecord
            {
                EntityId = _settings.CurrentServiceId,
                EntityType = nameof(ServiceData),
                Message = msg,
                Type = logType,
                ServiceId = _settings.CurrentServiceId,
                StatusCode = statusCode,
                Created = DateTime.UtcNow,
            });
        }
        await _context.SaveChangesdDirectly();
    }
}
