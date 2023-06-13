using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Linq.CompilerServices.TypeSystem;
using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Helpers;
internal class BinaryToObjectConverter : BinaryToObjectConverterAbstract
{

    public override byte[] ConvertToBinary(object obj)
    {
        try
        {
            //return MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Options);
            return MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Options);

        }
        catch (Exception ex)
        {
            throw new Exception($"Error when convert object of type `{obj?.GetType().FullName}` to binary", ex);
        }
    }

    public override object ConvertToObject(byte[] bytes)
    {
        try
        {
            return MessagePackSerializer.Deserialize<ExpandoObject>(bytes, ContractlessStandardResolver.Options);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error when convert bytes to ExpandoObject", ex);
        }
    }

    public override T ConvertToObject<T>(byte[] bytes)
    {
        try
        {
            return MessagePackSerializer.Deserialize<T>(bytes, ContractlessStandardResolver.Options);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error when convert bytes to `{typeof(T)}`", ex);
        }
    }

    public override object ConvertToObject(byte[] bytes, Type type)
    {
        try
        {
            return MessagePackSerializer.Deserialize(type, bytes, ContractlessStandardResolver.Options);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error when convert bytes to `{typeof(T)}`", ex);
        }
    }
}