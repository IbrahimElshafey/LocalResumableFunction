using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LocalResumableFunction.InOuts
{
    public class MethodData
    {
        public MethodData(MethodBase methodBase)
        {
            if (methodBase == null) return;
            //methodBase.Attributes= MethodAttributes.NewSlot;
            MethodName = methodBase.Name;
            ClassName = methodBase.DeclaringType?.FullName;
            AssemblyName = Path.GetFileName(methodBase.DeclaringType?.Assembly.Location);
            MethodSignature = CalcSignature(methodBase);
            CreateMethodHash();
        }

        public MethodData(MethodIdentifier methodIdentifier):this(methodIdentifier?.MethodInfo)
        {
        }

        public string AssemblyName { get; internal set; }
        public string ClassName { get; internal set; }
        public string MethodName { get; internal set; }
        public string MethodSignature { get; set; }
        public byte[] MethodHash { get; set; }

        internal static string CalcSignature(MethodBase value)
        {
            var parameterInfos = value.GetParameters();
            return parameterInfos.Length != 0
                ? parameterInfos
                    .Select(x => x.ParameterType.Name)
                    .Aggregate((x, y) => $"{x}#{y}")
                : string.Empty;
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
