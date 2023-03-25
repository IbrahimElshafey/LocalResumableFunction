namespace ResumableFunctions.Core.Attributes;

/// <summary>
/// Put this 
/// </summary>
public sealed class ResumableFunctionAttribute : Attribute
{
    public override object TypeId => nameof(ResumableFunctionAttribute);
}