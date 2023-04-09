namespace ResumableFunctions.Handler.InOuts;

public interface IEntity
{
    int Id { get; internal set; }
    DateTime Created { get; internal set; }
}

