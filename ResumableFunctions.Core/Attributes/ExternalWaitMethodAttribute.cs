namespace ResumableFunctions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public abstract class ExternalWaitMethodAttribute : Attribute, ITrackingIdetifier
{
    public ExternalWaitMethodAttribute(string assemblyName, string classFullName)
    {
        AssemblyName = assemblyName;
        ClassFullName = classFullName;
    }
    public ExternalWaitMethodAttribute(string trackingIdentifier)
    {
        TrackingIdentifier = trackingIdentifier;
    }
    public override object TypeId => nameof(ExternalWaitMethodAttribute);
    public string AssemblyName { get; }
    public string ClassFullName { get; }
    public string TrackingIdentifier { get; }
}



