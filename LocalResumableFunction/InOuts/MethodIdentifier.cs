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

    private MethodInfo? _methodInfo;
    internal MethodInfo? MethodInfo
    {
        get
        {
            if (AssemblyName != null && ClassName != null && MethodName != null && _methodInfo == null)
            {
                _methodInfo = Assembly.LoadFrom(AppContext.BaseDirectory + AssemblyName)
                     ?.GetType(ClassName)
                     ?.GetMethods()
                     .FirstOrDefault(x => x.Name == MethodName && CalcSignature(x) == MethodSignature);
                return _methodInfo;
            }
            return _methodInfo;
        }
    }

    internal void SetMethodInfo(MethodBase value)
    {
        MethodName = value.Name;
        ClassName = value.DeclaringType?.FullName;
        AssemblyName = Path.GetFileName(value.DeclaringType?.Assembly.Location);
        MethodSignature = CalcSignature(value);
        CreateMethodHash();
    }

    private string CalcSignature(MethodBase value)
    {
        var parameterInfos = value.GetParameters();
        return parameterInfos.Length != 0
            ? parameterInfos
                .Select(x => x.ParameterType.Name)
                .Aggregate((x, y) => $"{x}#{y}")
            : string.Empty;
    }

    internal void CreateMethodHash()
    {
        // Use input string to calculate MD5 hash
        var input = string.Concat(MethodName, ClassName, AssemblyName, MethodSignature);
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(input);
        MethodHash =  md5.ComputeHash(inputBytes);
    }
}