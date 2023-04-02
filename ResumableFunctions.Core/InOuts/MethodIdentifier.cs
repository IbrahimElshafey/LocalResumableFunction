using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Core.Helpers;

namespace ResumableFunctions.Core.InOuts;


public class MethodIdentifier
{

    private MethodInfo _methodInfo;
    public int Id { get; internal set; }

    //todo: to use later if method deletd from code
    public bool IsActiveInCode { get; internal set; } = true;

    public string AssemblyName { get; internal set; }
    public string ClassName { get; internal set; }
    public string MethodName { get; internal set; }
    public string MethodSignature { get; internal set; }
    public string TrackingId { get; internal set; }

    public byte[] MethodHash { get; set; }

    public MethodType Type { get; set; }
    public List<Wait> WaitsCreatedByFunction { get; internal set; }
    public List<MethodWait> WaitsRequestsForMethod { get; internal set; }
    public List<ResumableFunctionState> ActiveFunctionsStates { get; internal set; }

    internal MethodInfo MethodInfo
    {
        get
        {
            if (_methodInfo == null)
                _methodInfo = CoreExtensions.GetMethodInfo(AssemblyName, ClassName, MethodName, MethodSignature);
            return _methodInfo;
        }
    }

    public override string ToString()
    {
        return $"{AssemblyName} # {ClassName}{MethodName} # {MethodSignature}";
    }
}

