namespace ResumableFunctions.Core.Attributes;

public sealed class SubResumableFunctionAttribute : Attribute, ITrackingIdetifier
{
    public override object TypeId => nameof(SubResumableFunctionAttribute);
    public string TrackingIdetifier { get; set; }
}