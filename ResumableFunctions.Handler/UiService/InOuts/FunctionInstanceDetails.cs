﻿using ResumableFunctions.Handler.InOuts;
using System.Collections;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public class FunctionInstanceDetails
    {
        public string FunctionUrn { get;}
        public string FunctionName { get;}
        public long InstanceId { get; }
        public FunctionStatus Status { get; }
        public string InstanceData { get; }
        public DateTime Created { get; }
        public DateTime Modified { get; }
        public int ErrorsCount { get; }
        public ArrayList Waits { get; }
        public List<MethodWaitDetails> MethodWaitDetails { get; }
        public List<LogRecord> Logs { get; }
        public long FunctionId { get; }

        public FunctionInstanceDetails(
            long instanceId,long functionId, string name, string functionName, FunctionStatus status, string instanceData, DateTime created, DateTime modified, int errorsCount, ArrayList waits, List<LogRecord> logs)
        {
            InstanceId = instanceId;
            FunctionId = functionId;
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
