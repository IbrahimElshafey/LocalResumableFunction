namespace ResumableFunctions.Handler.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SubResumableFunctionAttribute : Attribute, ITrackingIdentifier
{

    public const string AttributeId = nameof(SubResumableFunctionAttribute);
    public override object TypeId => AttributeId;
    public string MethodUrn { get; }

    public SubResumableFunctionAttribute(string methodUrn)
    {
        MethodUrn = methodUrn;
    }
}