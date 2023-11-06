namespace ResumableFunctions.Handler.InOuts.Entities;
public interface IEntity
{
    DateTime Created { get; }
    int? ServiceId { get; }
}
public interface IEntity<IdType> : IEntity
{
    IdType Id { get; }
}

