using Newtonsoft.Json.Serialization;

namespace ResumableFunctions.Core.Helpers;

public class StateContractResolver : DefaultContractResolver
{
    //protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    //{
    //    var props = type.
    //        GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
    //        .Where(p =>
    //                p.Name != nameof(IEventData.EventProviderName) &&
    //                p.Name != nameof(IEventData.EventIdentifier) &&
    //                p.PropertyType != typeof(FunctionRuntimeInfo) &&
    //                !p.PropertyType.IsAssignableFrom(typeof(Expression))
    //            )
    //        .Select(p => base.CreateProperty(p, memberSerialization))
    //        .Union(
    //            type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
    //            .Select(f => base.CreateProperty(f, memberSerialization)))
    //        .ToList();
    //    props.ForEach(p => { p.Writable = true; p.Readable = true; });
    //    return props;
    //}
}