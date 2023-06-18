namespace ResumableFunctions.Handler.UiService.InOuts
{
    public record MainStatistics(
        int Services,
        int ResumableFunctions,
        int ResumableFunctionsInstances,
        int Methods,
        int PushedCalls,
        int LatestLogErrors);
}
