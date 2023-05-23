using System.ComponentModel.DataAnnotations.Schema;

namespace ResumableFunctions.Handler.InOuts;
public class ServiceData : ObjectWithLog, IEntityWithUpdate
{
    public int Id { get; internal set; }
    public DateTime Created { get; internal set; }
    public string AssemblyName { get; internal set; }
    public string Url { get; internal set; }

    [NotMapped]
    public string[] ReferencedDlls { get; internal set; }
    public DateTime Modified { get; internal set; }
    public int ParentId { get; internal set; }
    public string ConcurrencyToken { get; internal set; }

    public override void AddError(string message, Exception ex = null, string code = "")
    {
        base.AddError(message, ex, code);
        //refernce dlls logs to main
        Logs.Last().EntityId = ParentId == -1 ? Id : ParentId;
    }

    public override void AddLog(string message, LogType logType = LogType.Info, string code = "")
    {
        base.AddLog(message, logType, code);
        Logs.Last().EntityId = ParentId == -1 ? Id : ParentId;
    }

    internal int GetRootServiceId()
    {
        return ParentId == -1 ? Id : ParentId;
    }
}
