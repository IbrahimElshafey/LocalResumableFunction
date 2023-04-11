namespace ResumableFunctions.Handler.InOuts;

internal interface IEntityWithUpdate : IEntity
{
    DateTime Modified { get; }
    string ConcurrencyToken { get; }
}
