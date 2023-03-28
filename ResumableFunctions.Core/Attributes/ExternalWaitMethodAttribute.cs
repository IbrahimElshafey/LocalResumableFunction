namespace ResumableFunctions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class ExternalWaitMethodAttribute : Attribute
{
    public override object TypeId => nameof(ExternalWaitMethodAttribute);
    public string AssemblyName { get; set; }
    public string ClassFullName { get; set; }
}
