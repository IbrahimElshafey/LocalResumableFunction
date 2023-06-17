using System.ComponentModel.DataAnnotations.Schema;
using MessagePack;

namespace ResumableFunctions.Handler.InOuts;
public class ServiceData : IObjectWithLog, IEntityWithUpdate
{
    [IgnoreMember]
    public int ErrorCounter { get; set; }

    [IgnoreMember]
    [NotMapped]
    public List<LogRecord> Logs { get; } = new();
    public int Id { get; internal set; }
    public DateTime Created { get; internal set; }
    public int? ServiceId { get; internal set; }
    public string AssemblyName { get; internal set; }
    public string Url { get; internal set; }

    [NotMapped]
    public string[] ReferencedDlls { get; internal set; }
    public DateTime Modified { get; internal set; }
    public int ParentId { get; internal set; }
    public string ConcurrencyToken { get; internal set; }

    public void AddError(string message, Exception ex = null, string code = "")
    {
        (this as IObjectWithLog).AddError(message, ex, code);
        //refernce dlls logs to main
        Logs.Last().EntityId = ParentId == -1 ? Id : ParentId;
    }

    public void AddLog(string message, LogType logType = LogType.Info, string code = "")
    {
        (this as IObjectWithLog).AddLog(message, logType, code);
        Logs.Last().EntityId = ParentId == -1 ? Id : ParentId;
    }

    internal int GetRootServiceId()
    {
        return ParentId == -1 ? Id : ParentId;
    }
}
