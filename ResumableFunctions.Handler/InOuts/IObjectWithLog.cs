using System.ComponentModel.DataAnnotations.Schema;
using MessagePack;

namespace ResumableFunctions.Handler.InOuts;

public interface IObjectWithLog
{

    [IgnoreMember]
    public int ErrorCounter { get; internal set; }

    [IgnoreMember]
    [NotMapped]
    public List<LogRecord> Logs { get; set; }
}