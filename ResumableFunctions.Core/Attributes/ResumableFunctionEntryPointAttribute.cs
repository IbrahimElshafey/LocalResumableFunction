namespace ResumableFunctions.Core.Attributes;

/// <summary>
///     Start point for a resumable function
/// </summary>
/// 
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]

public sealed class ResumableFunctionEntryPointAttribute : Attribute, ITrackingIdetifier
{
    public override object TypeId => nameof(ResumableFunctionEntryPointAttribute);
    public string TrackingIdetifier { get; set; }
}