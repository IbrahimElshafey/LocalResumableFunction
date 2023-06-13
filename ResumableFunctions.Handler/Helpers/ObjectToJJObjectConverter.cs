using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace ResumableFunctions.Handler.Helpers;
public class ObjectToJJObjectConverter : ValueConverter<object, string>
{
    public ObjectToJJObjectConverter() : base(o => ObjectToJson(o), json => JsonToObject(json))
    {
    }

    private static object JsonToObject(string json)
    {
        return JsonConvert.DeserializeObject(json);
    }

    private static string ObjectToJson(object obj)
    {
        return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
    }
}
