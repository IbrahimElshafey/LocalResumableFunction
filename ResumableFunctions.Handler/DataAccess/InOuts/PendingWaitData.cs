using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess.InOuts
{
    public record PendingWaitData(long WaitId, WaitTemplate Template, string MandatoryPart, bool IsFirst);
}
