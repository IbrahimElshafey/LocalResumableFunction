using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler.InOuts.Entities;
public class PrivateData : IEntity<long>, IEntityWithUpdate, IAfterChangesSaved, IBeforeSaveEntity
{
    public PrivateData()
    {

    }
    public long Id { get; set; }
    public object Value { get; set; }
    public PrivateDataType Type { get; set; }
    public List<WaitEntity> ClosureLinkedWaits { get; set; }
    public List<WaitEntity> LocalsLinkedWaits { get; set; }

    public DateTime Created { get; set; }

    public int? ServiceId { get; set; }

    public DateTime Modified { get; set; }

    public string ConcurrencyToken { get; set; }
    public int? FunctionStateId { get; internal set; }

    public void AfterChangesSaved()
    {
        SetFunctionStateId();
    }

    public void BeforeSave()
    {
        SetFunctionStateId();
    }

    private void SetFunctionStateId()
    {
        if (FunctionStateId != null && FunctionStateId != 0) return;
        var stateId =
           LocalsLinkedWaits?.FirstOrDefault()?.FunctionStateId ??
           ClosureLinkedWaits?.FirstOrDefault()?.FunctionStateId;
        if (stateId == null || stateId == 0)
        {
            stateId =
            LocalsLinkedWaits?.FirstOrDefault()?.FunctionState?.Id ??
            ClosureLinkedWaits?.FirstOrDefault()?.FunctionState?.Id;
        }
        FunctionStateId = stateId;
    }

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
