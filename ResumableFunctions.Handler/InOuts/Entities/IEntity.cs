namespace ResumableFunctions.Handler.InOuts.Entities;
public interface IEntity<IdType>
{
    IdType Id { get; }
    DateTime Created { get; }
    int? ServiceId { get; }
}

