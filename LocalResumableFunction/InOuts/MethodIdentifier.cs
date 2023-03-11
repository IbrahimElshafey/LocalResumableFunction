using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction.InOuts;

[Index(nameof(MethodHash), IsUnique = true, Name = "Index_MethodHash")]
public class MethodIdentifier
{

    private MethodInfo _methodInfo;
    [Key] public int Id { get; internal set; }

    public string AssemblyName { get; internal set; }
    public string ClassName { get; internal set; }
    public string MethodName { get; internal set; }
    public string MethodSignature { get; internal set; }

    [MaxLength(16)] public byte[] MethodHash { get; set; }

    public MethodType Type { get; set; }
    public List<Wait> WaitsCreatedByFunction { get; internal set; }
    public List<MethodWait> WaitsRequestsForMethod { get; internal set; }
    public List<ResumableFunctionState> ActiveFunctionsStates { get; internal set; }

    internal MethodInfo MethodInfo
    {
        get
        {
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
}