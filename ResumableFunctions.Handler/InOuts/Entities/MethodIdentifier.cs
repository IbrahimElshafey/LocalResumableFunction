using ResumableFunctions.Handler.Helpers;
using System.Reflection;

namespace ResumableFunctions.Handler.InOuts.Entities;

public abstract class MethodIdentifier : IEntity<int>, IEntityWithUpdate
{
    private MethodInfo _methodInfo;

    public int Id { get; set; }
    public string AssemblyName { get; set; }
    public DateTime Created { get; set; }
    public string ClassName { get; set; }
    public string MethodName { get; set; }
    public string MethodSignature { get; set; }
    public byte[] MethodHash { get; set; }
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
    public int? ServiceId { get; set; }

    public DateTime Modified { get; set; }
    public string ConcurrencyToken { get; set; }

    public override string ToString()
    {
        return $"AssemblyName:{AssemblyName}, ClassName:{ClassName}, MethodName:{MethodName}, MethodSignature:{MethodSignature}";
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

