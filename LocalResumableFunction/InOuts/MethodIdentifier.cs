using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace LocalResumableFunction.InOuts
{
    public class MethodIdentifier
    {
        [Key]
        public int Id { get; internal set; }
        public string AssemblyName { get; internal set; }
        public string ClassName { get; internal set; }
        public string MethodName { get; internal set; }
        public string MethodSignature { get; set; }
        public List<Wait> WaitsCreatedByFunction { get; internal set; }
        public List<MethodWait> WaitsRequestsForMethod { get; internal set; }
        public List<ResumableFunctionState> ActiveFunctionsStates { get; internal set; }

        internal MethodBase GetMethodBase()
        {
            if (AssemblyName != null && ClassName != null && MethodName != null)
            {
                return Assembly.Load(AssemblyName)
                    ?.GetType(ClassName)
                    ?.GetMethod(MethodName);
            }
            return null;
        }

        internal void SetMethodBase(MethodBase value)
        {
           MethodName = value.Name;
           ClassName = value.DeclaringType?.FullName;
           AssemblyName = value.DeclaringType?.Assembly.FullName;
        }
    }
}