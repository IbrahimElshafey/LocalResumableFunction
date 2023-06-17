using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler.InOuts
{
    public class MethodData
    {
        private MethodInfo _methodInfo;

        [JsonConstructor]
        public MethodData(
            string assemblyName,
            string className,
            string methodName,
            string methodSignature,
            byte[] methodHash,
            string methodUrn,
            bool canPublishFromExternal,
            bool isActive)
        {
            AssemblyName = assemblyName;
            ClassName = className;
            MethodName = methodName;
            MethodSignature = methodSignature;
            MethodHash = methodHash;
            MethodUrn = methodUrn;
            CanPublishFromExternal = canPublishFromExternal;
            IsActive = isActive;
        }
        public MethodData()
        {

        }
        public MethodData(MethodInfo methodInfo)
        {
            if (methodInfo == null) return;
            AssemblyName = methodInfo.DeclaringType?.Assembly.GetName().Name;
            ClassName = methodInfo.DeclaringType?.FullName;
            MethodName = methodInfo.Name;
            MethodSignature = CalcSignature(methodInfo);
            MethodHash = GetMethodHash(MethodName, ClassName, AssemblyName, MethodSignature);
            MethodUrn = GetMethodUrn(methodInfo);
            CanPublishFromExternal = GetCanPublishFromExternal(methodInfo);
        }

        private bool GetCanPublishFromExternal(MethodInfo methodInfo)
        {
            var wma = methodInfo
              .GetCustomAttributes()
              .FirstOrDefault(
              attribute => attribute.TypeId == PushCallAttribute.AttributeId
              ) as PushCallAttribute;

            return wma?.CanPublishFromExternal ?? false;
        }

        public string MethodUrn { get; internal set; }
        public string AssemblyName { get; internal set; }
        public string ClassName { get; internal set; }
        public string MethodName { get; internal set; }
        public string MethodSignature { get; internal set; }
        public byte[] MethodHash { get; internal set; }
        private string GetMethodUrn(MethodInfo method)
        {
            var trackId = method
                .GetCustomAttributes()
                .FirstOrDefault(
                attribute =>
                    attribute.TypeId == ResumableFunctionEntryPointAttribute.AttributeId ||
                    attribute.TypeId == ResumableFunctionAttribute.AttributeId ||
                    attribute.TypeId == PushCallAttribute.AttributeId
                );

            return (trackId as ITrackingIdetifier)?.MethodUrn;
        }

        internal MethodInfo MethodInfo
        {
            get
            {
                if (_methodInfo == null)
                    _methodInfo = CoreExtensions.GetMethodInfo(AssemblyName, ClassName, MethodName, MethodSignature);
                return _methodInfo;
            }
        }

        public MethodIdentifier MethodIdentifier { get; internal set; }
        public int MethodIdentifierId { get; internal set; }
        public MethodType MethodType { get; internal set; }
        public bool CanPublishFromExternal { get; internal set; }
        public bool IsActive { get; internal set; } = true;

        internal static string CalcSignature(MethodBase value)
        {
            var parameterInfos = value.GetParameters();
            var inputs = parameterInfos.Length != 0
                ? parameterInfos
                    .Select(x => x.ParameterType.Name)
                    .Aggregate((x, y) => $"{x}#{y}")
                : string.Empty;
            if (value is MethodInfo methodInfo)
                return $"{methodInfo.ReturnType.Name}#{inputs}";
            return inputs;
        }

        internal static byte[] GetMethodHash(string MethodName, string ClassName, string AssemblyName, string MethodSignature)
        {
            var input = string.Concat(MethodName, ClassName, AssemblyName, MethodSignature);
            using var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            return md5.ComputeHash(inputBytes);
        }

        //internal MethodIdentifier ToMethodIdentifier()
        //{
        //    return new MethodIdentifier
        //    {
        //        MethodName = MethodName,
        //        MethodSignature = MethodSignature,
        //        AssemblyName = AssemblyName,
        //        ClassName = ClassName,
        //        MethodHash = MethodHash
        //    };
        //}

        public override string ToString()
        {
            return $"[{AssemblyName} # {ClassName}.{MethodName} # {MethodSignature}] with type {MethodType}";
        }

        internal bool Validate()
        {
            if (MethodUrn == null)
                throw new NullReferenceException($"For method ({this}) MethodUrn must not be null");
            return true;
        }
    }


}
