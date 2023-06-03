using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Helpers;

public abstract class BinaryToObjectConverter
{
    public virtual Expression<Func<object, byte[]>> ToBinary => ob => ConvertToBinary(ob);
    public virtual Expression<Func<byte[], object>> ToObject => bytes => ConvertToObject(bytes);

    public abstract byte[] ConvertToBinary(object ob);
    public abstract object ConvertToObject(byte[] bytes);
    public abstract T ConvertToObject<T>(byte[] bytes);
}
