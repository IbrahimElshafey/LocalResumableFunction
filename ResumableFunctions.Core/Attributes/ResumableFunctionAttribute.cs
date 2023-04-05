namespace ResumableFunctions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ResumableFunctionAttribute : Attribute, ITrackingIdetifier
{
    public override object TypeId => nameof(ResumableFunctionAttribute);
    public string TrackingIdentifier { get; set; }
}