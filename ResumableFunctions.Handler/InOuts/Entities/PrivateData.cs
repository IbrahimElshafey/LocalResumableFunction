using Newtonsoft.Json.Linq;

namespace ResumableFunctions.Handler.InOuts.Entities;

public class PrivateData : IEntity<Guid>, IEntityWithUpdate
{
    public Guid Id { get; set; }
    public object Value { get; set; }
    public List<WaitEntity> ClosureLinkedWaits { get; set; }
    public List<WaitEntity> LocalsLinkedWaits { get; set; }

    public DateTime Created { get; set; }

    public int? ServiceId { get; set; }

    public DateTime Modified { get; set; }

    public string ConcurrencyToken { get; set; }

    public T GetProp<T>(string propName)
    {
        switch (Value)
        {
            case JObject jobject:
                return jobject[propName].ToObject<T>();
            case object closureObject:
                return (T)closureObject.GetType().GetField(propName).GetValue(closureObject);
            default: return default;
        }
    }

    internal object AsType(Type closureClass)
    {
        Value = Value is JObject jobject ? jobject.ToObject(closureClass) : Value;
        return Value ?? Activator.CreateInstance(closureClass);
    }
}
