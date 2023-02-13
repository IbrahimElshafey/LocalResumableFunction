namespace LocalResumableFunction.Helpers;

/// <summary>
///     Start point for a resumable function
/// </summary>
public sealed class ResumableFunctionEntryPointAttribute : Attribute
{
    public override object TypeId => nameof(ResumableFunctionEntryPointAttribute);
    //todo:props to determine scan routine
}

public sealed class SubResumableFunctionAttribute : Attribute
{
    public override object TypeId => nameof(SubResumableFunctionAttribute);
}