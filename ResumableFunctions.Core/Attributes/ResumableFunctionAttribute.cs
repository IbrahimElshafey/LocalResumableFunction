namespace ResumableFunctions.Core.Attributes;

/// <summary>
/// Put this 
/// </summary>
public sealed class ResumableFunctionAttribute : Attribute, ITrackingIdetifier
{
    public override object TypeId => nameof(ResumableFunctionAttribute);

    public string TrackingIdetifier { get; set; }
}