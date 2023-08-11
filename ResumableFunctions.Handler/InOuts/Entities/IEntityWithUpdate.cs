namespace ResumableFunctions.Handler.InOuts.Entities;
public interface IEntityWithUpdate
{
    DateTime Modified { get; }
    string ConcurrencyToken { get; }
}