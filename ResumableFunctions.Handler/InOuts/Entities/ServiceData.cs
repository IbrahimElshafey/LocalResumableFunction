using System.ComponentModel.DataAnnotations.Schema;
using MessagePack;

namespace ResumableFunctions.Handler.InOuts.Entities;
public class ServiceData : IEntity<int>, IObjectWithLog, IEntityWithUpdate
{
    [IgnoreMember]
    [NotMapped]
    public List<LogRecord> Logs { get; set; } = new();
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public int? ServiceId { get; set; }
    public string AssemblyName { get; set; }
    public string Url { get; set; }

    [NotMapped]
    public string[] ReferencedDlls { get; set; }
    public DateTime Modified { get; set; }
    public int ParentId { get; set; }
    public string ConcurrencyToken { get; set; }

    public void AddError(string message, int code, Exception ex = null)
    {
        (this as IObjectWithLog).AddError(message, code, ex);
        Logs.Last().EntityId = ParentId == -1 ? Id : ParentId;
    }

    public void AddLog(string message, LogType logType, int code)
    {
        (this as IObjectWithLog).AddLog(message, logType, code);
        Logs.Last().EntityId = ParentId == -1 ? Id : ParentId;
    }
}
