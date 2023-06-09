using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Helpers;

public abstract class BinaryToObjectConverterAbstract
{
    public abstract byte[] ConvertToBinary(object ob);
    public abstract object ConvertToObject(byte[] bytes);
    public abstract T ConvertToObject<T>(byte[] bytes);
    public abstract object ConvertToObject(byte[] bytes, Type type);
}
