using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public class FunctionInstanceDetails
    {
        public string FunctionUrn { get;}
        public string FunctionName { get;}
        public int InstanceId { get; }
        public FunctionStatus Status { get; }
        public string InstanceData { get; }
        public DateTime Created { get; }
        public DateTime Modified { get; }
        public int ErrorsCount { get; }
        public List<Wait> Waits { get; }
        public List<LogRecord> Logs { get; }

        public FunctionInstanceDetails(
            int instanceId, string name, string functionName, FunctionStatus status, string instanceData, DateTime created, DateTime modified, int errorsCount, List<Wait> waits, List<LogRecord> logs)
        {
            InstanceId = instanceId;
            FunctionUrn = name;
            FunctionName = functionName;
            Status = status;
            InstanceData = instanceData;
            Created = created;
            Modified = modified;
            ErrorsCount = errorsCount;
            Waits = waits;
            Logs = logs;
        }
    }
}
