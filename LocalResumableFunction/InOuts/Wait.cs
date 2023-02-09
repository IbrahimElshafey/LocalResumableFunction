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



        [NotMapped]
        public MethodBase CallerMethodInfo
        {
            get
            {
                if (_callerMethodInfo != null)
                    return _callerMethodInfo;
                else if (AssemblyName != null && ClassName != null && MethodName != null)
                {
                    return Assembly.Load(AssemblyName)
                        ?.GetType(ClassName)
                        ?.GetMethod(MethodName);
                }
                return null;
            }

            internal set
            {
                _callerMethodInfo = value;
                if (value != null)
                {
                    MethodName = value.Name;
                    ClassName = value.DeclaringType?.FullName;
                    AssemblyName = value.DeclaringType?.Assembly.FullName;
                }
            }
        }
        internal string MethodName { get; set; }
        internal string ClassName { get; set; }
        internal string AssemblyName { get; set; }
        private ResumableFunctionLocal _currntFunction;
        [NotMapped]
        public ResumableFunctionLocal CurrntFunction
        {
            get
            {
                if (FunctionRuntimeInfo is not null)
                    if (FunctionRuntimeInfo.FunctionState is JObject stateAsJson)
                    {
                        var result = stateAsJson.ToObject(FunctionRuntimeInfo.InitiatedByClassType);
                        FunctionRuntimeInfo.FunctionState = result;
                        _currntFunction = (ResumableFunctionLocal)result;
                        return _currntFunction;
                    }
                    else if (FunctionRuntimeInfo.FunctionState is ResumableFunctionLocal)
                        return _currntFunction;
                return _currntFunction;
            }
            set => _currntFunction = value;
        }

        [JsonIgnore]
        public FunctionRuntimeInfo FunctionRuntimeInfo { get; internal set; }

        [ForeignKey(nameof(FunctionRuntimeInfo))]
        public int FunctionRuntimeInfoId { get; internal set; }
    }
}