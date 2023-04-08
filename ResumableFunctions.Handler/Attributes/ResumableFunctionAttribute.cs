namespace ResumableFunctions.Handler.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ResumableFunctionAttribute : Attribute, ITrackingIdetifier
{

    public const string AttributeId = nameof(ResumableFunctionAttribute);
    public override object TypeId => AttributeId;
    public string MethodUrn { get; }

    public ResumableFunctionAttribute(string methodUrn)
    {
        MethodUrn = methodUrn;
    }
}