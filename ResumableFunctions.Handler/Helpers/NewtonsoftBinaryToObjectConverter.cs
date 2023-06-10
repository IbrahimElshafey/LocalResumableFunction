//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Bson;
//using Newtonsoft.Json.Linq;
//using System.Collections;
//using System.Linq.Expressions;
//using System.Runtime.ExceptionServices;

//namespace ResumableFunctions.Handler.Helpers;

//internal class BinaryToObjectConverter1 : BinaryToObjectConverterAbstract
//{

//    public override byte[] ConvertToBinary(object obj)
//    {
//        try
//        {
//            if (obj == null) return null;
//            MemoryStream ms = new MemoryStream();
//            using var writer = new BsonDataWriter(ms);
//            var serializer = new JsonSerializer();
//            serializer.Serialize(writer, obj);
//            return ms.ToArray();
//        }
//        catch (Exception ex)
//        {
//            throw new Exception($"Error when convert object of type `{obj?.GetType().FullName}` to binary", ex);
//        }

//    }

//    public override object ConvertToObject(byte[] bytes, Type type)
//    {
//        try
//        {
//            if (bytes == null) return null;
//            MemoryStream ms = new MemoryStream(bytes);
//            using BsonDataReader reader = new BsonDataReader(ms);
//            var serializer = new JsonSerializer();
//            return serializer.Deserialize(reader, type);
//        }
//        catch (Exception ex)
//        {
//            throw new Exception($"Error when convert bytes to JObject", ex);
//        }
//    }
//    public override object ConvertToObject(byte[] bytes)
//    {
//        try
//        {
//            if (bytes == null) return null;
//            MemoryStream ms = new MemoryStream(bytes);
//            using BsonDataReader reader = new BsonDataReader(ms);
//            var serializer = new JsonSerializer();
//            return serializer.Deserialize<JObject>(reader);
//        }
//        catch (Exception ex)
//        {
//            throw new Exception($"Error when convert bytes to JObject", ex);
//        }

//    }

//    public override T ConvertToObject<T>(byte[] bytes)
//    {
//        try
//        {
//            if (bytes == null) return default;
//            MemoryStream ms = new MemoryStream(bytes);
//            using BsonDataReader reader = new BsonDataReader(ms);
//            var serializer = new JsonSerializer();
//            return serializer.Deserialize<T>(reader);
//        }
//        catch (Exception ex)
//        {
//            throw new Exception($"Error when convert bytes to `{typeof(T)}`", ex);
//        }
//    }
//}
