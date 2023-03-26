namespace ResumableFunctions.Core.Attributes;

/// <summary>
///     Start point for a resumable function
/// </summary>
public sealed class ResumableFunctionEntryPointAttribute : Attribute
{
    public override object TypeId => nameof(ResumableFunctionEntryPointAttribute);
}