using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler.InOuts;

public abstract class MethodIdentifier
{
    private MethodInfo _methodInfo;

    //private MethodInfo _methodInfo;
    public int Id { get; internal set; }

    //editable props
    public string AssemblyName { get; internal set; }
    public string ClassName { get; internal set; }
    public string MethodName { get; internal set; }
    public string MethodSignature { get; internal set; }
    public byte[] MethodHash { get; internal set; }
    public MethodType Type { get; set; }

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
        return $"{AssemblyName} # {ClassName}.{MethodName} # {MethodSignature}";
    }

    internal virtual void FillFromMethodData(MethodData methodData)
    {
        if (methodData == null) return;
        Type = methodData.MethodType;
        AssemblyName = methodData.AssemblyName;
        ClassName = methodData.ClassName;
        MethodName = methodData.MethodName;
        MethodSignature = methodData.MethodSignature;
        MethodHash = methodData.MethodHash;
    }
}

