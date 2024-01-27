using ResumableFunctions.Data.Abstraction.Entities.EntityBehavior;
using System;

namespace ResumableFunctions.Data.Abstraction.Entities.EntityBehaviour
{
    public interface IEntity<IdType> : IEntity
    {
        IdType Id { get; }
    }
}
