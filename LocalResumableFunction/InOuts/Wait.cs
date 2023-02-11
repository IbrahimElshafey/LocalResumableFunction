using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;

namespace LocalResumableFunction.InOuts
{
    public abstract class Wait
    {
        private MethodBase _callerMethodInfo;

        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public WaitStatus Status { get; internal set; }
        public bool IsFirst { get; internal set; }
        public bool IsSingle { get; internal set; } = true;
        public int StateAfterWait { get; internal set; }
        public bool IsNode { get; internal set; }
        public ReplayType? ReplayType { get; internal set; }
        public WaitType WaitType { get; internal set; }

       
        [JsonIgnore]
        internal ResumableFunctionState FunctionState { get;  set; }

        internal int FunctionStateId { get; set; }

        internal MethodIdentifier WaitMethodIdentifier { get; set; }

        internal int WaitMethodIdentifierId { get; set; }

        internal MethodIdentifier InitiatedByMethod { get; set; }

        internal int InitiatedByMethodId { get; set; }

        internal Wait ParentFunctionWait { get; set; }

        internal int? ParentFunctionWaitId { get; set; }


        //[NotMapped]
        //public MethodBase CallerMethodInfo
        //{
        //    get
        //    {
        //        if (_callerMethodInfo != null)
        //            return _callerMethodInfo;
        //        else return WaitMethodIdentifier?.GetMethodBase();
        //    }

        //    internal set
        //    {
        //        _callerMethodInfo = value;
        //        WaitMethodIdentifier?.SetMethodBase(_callerMethodInfo);
        //    }
        //}

        private ResumableFunctionLocal _currntFunction;

        [NotMapped]
        internal ResumableFunctionLocal CurrntFunction
        {
            get
            {
                if (FunctionState is not null)
                    if (FunctionState.StateObject is JObject stateAsJson)
                    {
                        var result = stateAsJson.ToObject(FunctionState.ResumableFunctionMethodInfo.DeclaringType);
                        FunctionState.StateObject = result;
                        _currntFunction = (ResumableFunctionLocal)result;
                        return _currntFunction;
                    }
                    else if (FunctionState.StateObject is ResumableFunctionLocal)
                        return _currntFunction;
                return _currntFunction;
            }
            set => _currntFunction = value;
        }

    }
}