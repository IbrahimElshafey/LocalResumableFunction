namespace ResumableFunctions.Handler.Attributes;

/// <summary>
///     Start point for a resumable function
/// </summary>
/// 
[AttributeUsage(AttributeTargets.Method)]

public sealed class ResumableFunctionEntryPointAttribute : Attribute, ITrackingIdetifier
{
    public const string AttributeId = nameof(ResumableFunctionEntryPointAttribute);
    public override object TypeId => AttributeId;
    public string MethodUrn { get; }
    public bool IsActive { get; }

    public ResumableFunctionEntryPointAttribute(string methodUrn, bool isActive = true)
    {
        MethodUrn = methodUrn;
        IsActive = isActive;
    }
}