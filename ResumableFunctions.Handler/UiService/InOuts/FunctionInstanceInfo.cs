using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public record FunctionInstanceInfo(ResumableFunctionState FunctionState, Wait CurrentWait, int WaitsCount)
    {
        public string StateColor => FunctionState.Status switch
        {
            FunctionStatus.New => "black",
            FunctionStatus.InProgress => "yellow",
            FunctionStatus.Completed => "green",
            FunctionStatus.Error => "red",
        };
    }
}
