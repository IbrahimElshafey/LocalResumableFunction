using Newtonsoft.Json.Linq;

namespace ResumableFunctions.Handler.InOuts.Entities;

public class RuntimeClosure : IEntity<Guid>, IEntityWithUpdate
{
    public Guid Id { get; set; }
    public string CallerName { get; set; }
    public object Value { get; set; }
    public List<WaitEntity> LinkedWaits { get; set; }

    public DateTime Created { get; set; }

    public int? ServiceId { get; set; }

    public DateTime Modified { get; set; }

    public string ConcurrencyToken { get; set; }

    internal object AsType(Type closureClass)
    {
        Value = Value is JObject jobject ? jobject.ToObject(closureClass) : Value;
        return Value ?? Activator.CreateInstance(closureClass);
    }
}
