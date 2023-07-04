using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public record FunctionInstanceInfo(ResumableFunctionState FunctionState, Wait CurrentWait, int WaitsCount, int Id)
    {
        public string StateColor => FunctionState.Status switch
        {
            FunctionStatus.New => "black",
            FunctionStatus.InProgress => "yellow",
            FunctionStatus.Completed => "green",
            FunctionStatus.InError => "red",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
