namespace ResumableFunctions.Handler.InOuts;

internal interface IEntity
{
    int Id { get; }
    DateTime Created { get; }
    int? ServiceId { get; }
}

