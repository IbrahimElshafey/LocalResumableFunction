namespace ResumableFunctions.Handler.Attributes;

/// <summary>
///     Start point for a resumable function
/// </summary>
/// 
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]

public sealed class ResumableFunctionEntryPointAttribute : Attribute, ITrackingIdetifier
{
    public override object TypeId => nameof(ResumableFunctionEntryPointAttribute);
    public string TrackingIdentifier { get; set; }
}