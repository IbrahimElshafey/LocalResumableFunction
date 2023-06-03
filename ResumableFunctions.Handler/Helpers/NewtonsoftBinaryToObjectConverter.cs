using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Helpers;

internal class NewtonsoftBinaryToObjectConverter : BinaryToObjectConverter
{
    private readonly ILogger<NewtonsoftBinaryToObjectConverter> _logger;
    public NewtonsoftBinaryToObjectConverter(ILogger<NewtonsoftBinaryToObjectConverter> logger)
    {
        _logger = logger;
    }
    public override byte[] ConvertToBinary(object obj)
    {
        try
        {
            MemoryStream ms = new MemoryStream();
            using var writer = new BsonDataWriter(ms);
            var serializer = new JsonSerializer();
            serializer.Serialize(writer, obj);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when convert object of type `{obj?.GetType().FullName}` to binary", ex);
            throw;
        }
       
    }

    public override object ConvertToObject(byte[] b)
    {
        try
        {
            MemoryStream ms = new MemoryStream(b);
            using BsonDataReader reader = new BsonDataReader(ms);
            var serializer = new JsonSerializer();
            return serializer.Deserialize<JObject>(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when convert bytes to JObject", ex);
            throw;
        }
       
    }

    public override T ConvertToObject<T>(byte[] bytes)
    {
        try
        {
            MemoryStream ms = new MemoryStream(bytes);
            using BsonDataReader reader = new BsonDataReader(ms);
            var serializer = new JsonSerializer();
            return serializer.Deserialize<T>(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when convert bytes to `{typeof(T)}`", ex);
            throw;
        }
    }
}
