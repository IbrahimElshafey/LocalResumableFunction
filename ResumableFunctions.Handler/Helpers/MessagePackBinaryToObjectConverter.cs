using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Helpers;
internal class MessagePackBinaryToObjectConverter : IBinaryToObjectConverter
{
    private readonly ILogger<MessagePackBinaryToObjectConverter> _logger;

    public MessagePackBinaryToObjectConverter(ILogger<MessagePackBinaryToObjectConverter> logger)
    {
        _logger = logger;
    }
    public Expression<Func<object, byte[]>> ToBinary => o => ObjectToBinary(o);

    public Expression<Func<byte[], object>> ToObject => b => BinaryToObject(b);

    internal object BinaryToObject(byte[] binary)
    {
        try
        {
            return MessagePackSerializer.Deserialize<ExpandoObject>(
              binary, ContractlessStandardResolver.Options);

        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when convert bytes to ExpandoObject", ex);
            throw;
        }

    }

    internal byte[] ObjectToBinary(object obj)
    {
        try
        {
            //return MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Options);
            return MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Options);

        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when convert object of type `{obj?.GetType().FullName}` to binary", ex);
            throw;
        }
    }
}