using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction.InOuts;

[Index(nameof(MethodHash), IsUnique = true, Name = "Index_MethodHash")]
public class MethodIdentifier
{
    [Key] public int Id { get; internal set; }

    public string AssemblyName { get; internal set; }
    public string ClassName { get; internal set; }
    public string MethodName { get; internal set; }
    public string MethodSignature { get; set; }

    [MaxLength(16)] public byte[] MethodHash { get; set; }

    public MethodType Type { get; set; }
    public List<Wait> WaitsCreatedByFunction { get; internal set; }
    public List<MethodWait> WaitsRequestsForMethod { get; internal set; }
    public List<ResumableFunctionState> ActiveFunctionsStates { get; internal set; }

    internal MethodBase GetMethodBase()
    {
        if (AssemblyName != null && ClassName != null && MethodName != null)
            return Assembly.Load(AssemblyName)
                ?.GetType(ClassName)
                ?.GetMethod(MethodName);
        return null;
    }

    internal void SetMethodBase(MethodBase value)
    {
        MethodName = value.Name;
        ClassName = value.DeclaringType?.FullName;
        AssemblyName = value.DeclaringType?.Assembly.FullName;
        var parameterInfos = value
            .GetParameters();
        MethodSignature = parameterInfos.Length != 0
            ? parameterInfos
                .Select(x => x.ParameterType.Name)
                .Aggregate((x, y) => $"{x}#{y}")
            : string.Empty;
        MethodHash = CreateMd5();
    }

    private byte[] CreateMd5()
    {
        // Use input string to calculate MD5 hash
        var input = string.Concat(MethodName, ClassName, AssemblyName, MethodSignature);
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(input);
        return md5.ComputeHash(inputBytes);
    }
}