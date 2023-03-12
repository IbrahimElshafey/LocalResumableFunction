namespace LocalResumableFunction.Attributes;

public sealed class SubResumableFunctionAttribute : Attribute
{
    public override object TypeId => nameof(SubResumableFunctionAttribute);
}