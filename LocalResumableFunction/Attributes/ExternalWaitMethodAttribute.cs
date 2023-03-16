namespace LocalResumableFunction.Attributes;

public sealed class ExternalWaitMethodAttribute : Attribute
{
    public string AssemblyName { get; set; }
    public string ClassName { get; set; }
}
