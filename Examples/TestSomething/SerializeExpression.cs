using MemoryPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Linq.Expressions;
using System.Linq.Expressions.Bonsai.Serialization;

namespace TestSomething
{
    internal class SerializeExpression
    {
        public void Run()
        {
            //UseSerialize_Linq();
            UseNuqleon();
        }

        private void UseNuqleon()
        {
            Expression<Func<int, int, string>> exp = (x, y) => (x + y * 10 - 30).ToString();
            var serializer = new BonsaiExpressionSerializer();
            var expSlim = exp.ToExpressionSlim();
            var serailzed = serializer.Serialize(expSlim);
            var back = serializer.Deserialize(serailzed);
            var exp2 = back.ToExpression();
        }

        //private void UseSerialize_Linq()
        //{
        //    Expression<Func<int, int, string>> exp = (x, y) => (x + y * 10 - 30).ToString();
        //    using var ms = new MemoryStream();
        //    var serializer = new ExpressionSerializer(new BinarySerializer());
        //    serializer.Serialize(ms, exp);
        //    var bytes = ms.ToArray();
        //}
    }
}
