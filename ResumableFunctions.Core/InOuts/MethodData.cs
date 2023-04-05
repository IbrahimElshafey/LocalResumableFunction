using ResumableFunctions.Core.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ResumableFunctions.Core.Helpers;

namespace ResumableFunctions.Core.InOuts
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
            string trackingId)
        {
            AssemblyName = assemblyName;
            ClassName = className;
            MethodName = methodName;
            MethodSignature = methodSignature;
            MethodHash = methodHash;
            TrackingId = trackingId;
        }

        public MethodData(MethodBase externalMethod, ExternalWaitMethodAttribute externalWaitMethodAttribute)
        {
            ClassName = externalWaitMethodAttribute.ClassFullName ?? externalMethod.DeclaringType?.FullName;
            AssemblyName = externalWaitMethodAttribute.AssemblyName ?? externalMethod.DeclaringType?.Assembly.GetName().Name;
            MethodName = externalMethod.Name;
            MethodSignature = CalcSignature(externalMethod);
            MethodHash = GetMethodHash(MethodName, ClassName, AssemblyName, MethodSignature);
            TrackingId = externalWaitMethodAttribute.TrackingIdentifier;
        }

        public MethodData(MethodBase methodBase)
        {
            if (methodBase == null) return;
            //methodBase.Attributes= MethodAttributes.NewSlot;
            MethodName = methodBase.Name;
            ClassName = methodBase.DeclaringType?.FullName;
            AssemblyName = methodBase.DeclaringType?.Assembly.GetName().Name;
            MethodSignature = CalcSignature(methodBase);
            MethodHash = GetMethodHash(MethodName, ClassName, AssemblyName, MethodSignature);
        }

        public string TrackingId { get; internal set; }
        public string AssemblyName { get; internal set; }
        public string ClassName { get; internal set; }
        public string MethodName { get; internal set; }
        public string MethodSignature { get; internal set; }
        public byte[] MethodHash { get; internal set; }

        internal MethodInfo MethodInfo
        {
            get
            {
                if (_methodInfo == null)
                    _methodInfo = CoreExtensions.GetMethodInfo(AssemblyName, ClassName, MethodName, MethodSignature);
                return _methodInfo;
            }
        }

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

        internal MethodIdentifier ToMethodIdentifier()
        {
            return new MethodIdentifier
            {
                MethodName = MethodName,
                MethodSignature = MethodSignature,
                AssemblyName = AssemblyName,
                ClassName = ClassName,
                MethodHash = MethodHash
            };
        }

        public override string ToString()
        {
            return $"{AssemblyName} # {ClassName}.{MethodName} # {MethodSignature}";
        }
    }


}
