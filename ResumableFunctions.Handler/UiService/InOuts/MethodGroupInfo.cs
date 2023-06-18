namespace ResumableFunctions.Handler.UiService.InOuts
{
    public record MethodGroupInfo(
        int Id, string URN, int MethodsCount,int ActiveWaits,int CompletedWaits,int CanceledWaits,DateTime Created);
}
