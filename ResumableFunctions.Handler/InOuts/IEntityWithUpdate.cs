namespace ResumableFunctions.Handler.InOuts;

public interface IEntityWithUpdate:IEntity
{
    DateTime Modified { get; internal set; }
    string Version { get; internal set; }
}

