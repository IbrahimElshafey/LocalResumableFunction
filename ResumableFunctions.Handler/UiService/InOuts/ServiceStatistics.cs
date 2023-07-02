namespace ResumableFunctions.Handler.UiService.InOuts
{
    public record ServiceStatistics(long Id, string ServiceName, int ErrorCounter, int FunctionsCount, int MethodsCount);
}
