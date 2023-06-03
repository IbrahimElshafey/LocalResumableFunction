using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Helpers;

public interface IBinaryToObjectConverter
{
    Expression<Func<object, byte[]>> ToBinary { get; }
    Expression<Func<byte[],object>> ToObject { get; }
}
