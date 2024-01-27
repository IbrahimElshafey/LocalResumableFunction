using System;

namespace ResumableFunctions.Data.Abstraction.Entities.EntityBehavior
{
    public interface IEntityWithUpdate
    {
        DateTime Modified { get; }
        string ConcurrencyToken { get; }
    }
}