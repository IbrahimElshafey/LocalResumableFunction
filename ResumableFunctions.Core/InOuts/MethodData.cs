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

namespace ResumableFunctions.Core.InOuts
{
    public class MethodData
    {
        private MethodInfo _methodInfo;

        [JsonConstructor]
        public MethodData(string assemblyName, string className, string methodName, string methodSignature, byte[] methodHash)
        {
            AssemblyName = assemblyName;
            ClassName = className;
            MethodName = methodName;
            MethodSignature = methodSignature;
            MethodHash = methodHash;
        }
        public MethodData(MethodBase externalMethod, ExternalWaitMethodAttribute externalWaitMethodAttribute)
        {
            ClassName = externalWaitMethodAttribute.ClassName ?? externalMethod.DeclaringType?.FullName;
            AssemblyName = externalWaitMethodAttribute.AssemblyName ?? externalMethod.DeclaringType?.Assembly.GetName().Name;
            MethodName = externalMethod.Name;
            MethodSignature = CalcSignature(externalMethod);
            CreateMethodHash();
        }

        public MethodData(MethodBase methodBase)
        {
            if (methodBase == null) return;
            //methodBase.Attributes= MethodAttributes.NewSlot;
            MethodName = methodBase.Name;
            ClassName = methodBase.DeclaringType?.FullName;
            AssemblyName = methodBase.DeclaringType?.Assembly.GetName().Name;
            MethodSignature = CalcSignature(methodBase);
            CreateMethodHash();
        }

        public string AssemblyName { get; internal set; }
        public string ClassName { get; internal set; }
        public string MethodName { get; internal set; }
        public string MethodSignature { get; internal set; }
        public byte[] MethodHash { get; internal set; }

        //todo:refactor copied code from MethodIdentifier
        internal MethodInfo MethodInfo
        {
            get
            {
                if (File.Exists($"{AppContext.BaseDirectory}{AssemblyName}.dll"))
                    if (AssemblyName != null && ClassName != null && MethodName != null && _methodInfo == null)
                    {
                        _methodInfo = Assembly.LoadFrom(AppContext.BaseDirectory + AssemblyName)
                            .GetType(ClassName)
                            ?.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .FirstOrDefault(x => x.Name == MethodName && MethodData.CalcSignature(x) == MethodSignature);
                        return _methodInfo;
                    }

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

        private void CreateMethodHash()
        {
            var input = string.Concat(MethodName, ClassName, AssemblyName, MethodSignature);
            using var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            MethodHash = md5.ComputeHash(inputBytes);
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
    }


}
