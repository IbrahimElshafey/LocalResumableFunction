using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Helpers;

internal class NewtonsoftBinaryToObjectConverter : IBinaryToObjectConverter
{
    public Expression<Func<object, byte[]>> ToBinary => o => ObjectToBinary(o);
    public Expression<Func<byte[], object>> ToObject => b => BinaryToObject(b);

    private byte[] ObjectToBinary(object o)
    {
        MemoryStream ms = new MemoryStream();
        using var writer = new BsonDataWriter(ms);
        var serializer = new JsonSerializer();
        serializer.Serialize(writer, o);
        return ms.ToArray();
    }

    private object BinaryToObject(byte[] b)
    {
        MemoryStream ms = new MemoryStream(b);
        using BsonDataReader reader = new BsonDataReader(ms);
        var serializer = new JsonSerializer();
        return serializer.Deserialize<JObject>(reader);
    }
}
