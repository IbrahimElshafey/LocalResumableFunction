namespace ResumableFunctions.Handler.InOuts;

public interface IEntity
{
    long Id { get; }
    DateTime Created { get; }
    long? ServiceId { get; }
}

