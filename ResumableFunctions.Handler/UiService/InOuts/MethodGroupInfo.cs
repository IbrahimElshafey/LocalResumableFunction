using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public record MethodInGroupInfo(string ServiceName, WaitMethodIdentifier Method,string GroupUrn);

    public record MethodGroupInfo(
        long Id, string URN, int MethodsCount, int ActiveWaits, int CompletedWaits, int CanceledWaits, DateTime Created)
    {
        public int AllWaitsCount => ActiveWaits + CompletedWaits + CanceledWaits;
    }
}
