//using MessagePack;
//using MessagePack.Resolvers;
//using Microsoft.Extensions.Logging;
//using System.Dynamic;
//using System.Linq.Expressions;

//namespace ResumableFunctions.Handler.Helpers;
//internal class MessagePackBinaryToObjectConverter : BinaryToObjectConverter
//{
//    private readonly ILogger<MessagePackBinaryToObjectConverter> _logger;

//    public MessagePackBinaryToObjectConverter(ILogger<MessagePackBinaryToObjectConverter> logger)
//    {
//        _logger = logger;
//    }

//    public override byte[] ConvertToBinary(object obj)
//    {
//        try
//        {
//            //return MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Options);
//            return MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Options);

//        }
//        catch (Exception ex)
//        {
//            _logger.LogError($"Error when convert object of type `{obj?.GetType().FullName}` to binary", ex);
//            throw;
//        }
//    }

//    public override object ConvertToObject(byte[] bytes)
//    {
//        try
//        {
//            return MessagePackSerializer.Deserialize<ExpandoObject>(
//              bytes, ContractlessStandardResolver.Options);

//        }
//        catch (Exception ex)
//        {
//            _logger.LogError($"Error when convert bytes to ExpandoObject", ex);
//            throw;
//        }
//    }

//    public override T ConvertToObject<T>(byte[] bytes)
//    {
//        try
//        {
//            return MessagePackSerializer.Deserialize<T>(
//              bytes, ContractlessStandardResolver.Options);

//        }
//        catch (Exception ex)
//        {
//            _logger.LogError($"Error when convert bytes to `{typeof(T)}`", ex);
//            throw;
//        }
//    }
//}