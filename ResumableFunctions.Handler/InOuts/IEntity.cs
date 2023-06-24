namespace ResumableFunctions.Handler.InOuts;

public interface IEntity
{
    int Id { get; }
    DateTime Created { get; }
    int? ServiceId { get; }
}

