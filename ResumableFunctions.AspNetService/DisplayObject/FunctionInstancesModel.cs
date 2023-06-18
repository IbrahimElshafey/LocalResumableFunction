using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.UiService.InOuts;

namespace ResumableFunctions.AspNetService.DisplayObject
{
    public class FunctionInstancesModel
    {
        public string FunctionName { get; set; }
        public List<FunctionInstanceInfo> Instances { get; set; }

        public int InProgressCount => Instances.Count(x => x.FunctionState.Status == FunctionStatus.InProgress);
        public int FailedCount => Instances.Count(x => x.FunctionState.Status == FunctionStatus.Error);
        public int CompletedCount => Instances.Count(x => x.FunctionState.Status == FunctionStatus.Completed);
    }
}