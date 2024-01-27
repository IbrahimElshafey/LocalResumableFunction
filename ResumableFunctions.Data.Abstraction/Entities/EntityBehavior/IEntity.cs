using System;

namespace ResumableFunctions.Data.Abstraction.Entities.EntityBehavior
{
    public interface IEntity
    {
        DateTime Created { get; }
        int? ServiceId { get; }
    }
}
