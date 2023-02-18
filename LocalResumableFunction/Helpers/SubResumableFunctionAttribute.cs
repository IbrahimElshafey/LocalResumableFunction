namespace LocalResumableFunction.Helpers;

public sealed class SubResumableFunctionAttribute : Attribute
{
    public override object TypeId => nameof(SubResumableFunctionAttribute);
}