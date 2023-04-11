namespace ResumableFunctions.Handler.InOuts;

internal interface IEntityWithDelete : IEntity
{
    bool IsDeleted { get; }
}   