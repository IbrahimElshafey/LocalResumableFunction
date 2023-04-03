namespace ResumableFunctions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ExternalWaitMethodAttribute : Attribute, ITrackingIdetifier
{
    public ExternalWaitMethodAttribute(string assemblyName, string classFullName)
    {
        AssemblyName = assemblyName;
        ClassFullName = classFullName;
    }
    public ExternalWaitMethodAttribute(string trackingIdetifier)
    {
        TrackingIdetifier = trackingIdetifier;
    }
    public override object TypeId => nameof(ExternalWaitMethodAttribute);
    public string AssemblyName { get; }
    public string ClassFullName { get; }
    public string TrackingIdetifier { get; }
}

