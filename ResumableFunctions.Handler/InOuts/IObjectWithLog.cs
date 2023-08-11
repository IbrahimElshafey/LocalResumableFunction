using System.ComponentModel.DataAnnotations.Schema;
using MessagePack;
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.InOuts;

public interface IObjectWithLog
{
    [IgnoreMember]
    [NotMapped]
    public List<LogRecord> Logs { get; set; }
}