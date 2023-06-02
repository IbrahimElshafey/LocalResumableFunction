using Serialize.Linq.Interfaces;
using Serialize.Linq.Serializers;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
namespace TestSomething
{
    internal class SerializeExpression
    {
        public void Run()
        {
            Expression<Func<int, int, string>> exp = (x, y) => (x + y * 10 - 30).ToString();
            using var ms = new MemoryStream();
            var serializer = new ExpressionSerializer(new BinarySerializer());
            serializer.Serialize(ms, exp);
            var bytes = ms.ToArray();
        }

    }
}
