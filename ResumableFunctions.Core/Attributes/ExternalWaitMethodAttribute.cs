namespace ResumableFunctions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ExternalWaitMethodAttribute : Attribute, ITrackingIdetifier
{
    public override object TypeId => nameof(ExternalWaitMethodAttribute);
    public string AssemblyName { get; set; }
    public string ClassFullName { get; set; }
    public string TrackingIdetifier { get; set; }
}
