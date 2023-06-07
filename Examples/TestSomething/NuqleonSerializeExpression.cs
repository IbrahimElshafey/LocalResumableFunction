using MemoryPack;
using MessagePack.Resolvers;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Linq.Expressions;
using System.Linq.Expressions.Bonsai.Serialization;
using ResumableFunctions.Handler.InOuts;
using FastExpressionCompiler;
using System;
using Nuqleon.Json.Serialization;
using Json = Nuqleon.Json.Expressions;

namespace TestSomething
{
    internal class NuqleonSerializeExpression
    {
        public void Run()
        {
            //UseSerialize_Linq();
            UseNuqleon();
        }

        private void UseNuqleon()
        {
            Expression<Func<InputComplex, WaitExtraData, string>> exp = (x, y) => (x.Id + y.JobId.Length * 10 - 30).ToString();
            var obj = new ObjectSerializer();
            var serializer = new ExpressionSlimBonsaiSerializer(
                obj.GetJsonSerializer,
                obj.GetJsonDeserializer,
                new Version("0.9"));
            var expSlim = exp.ToExpressionSlim();
            var testSerializer = new ExpressionSerializer();
            var serailzed = testSerializer.Serialize(expSlim);
            var back = testSerializer.Deserialize(serailzed);

            var code = back.ToCSharpString();
            var exp2 = (LambdaExpression)back.ToExpression();



            var inputComplex = new InputComplex { Id = 1, Name = "kjkil" };
            var extraData = new WaitExtraData { JobId = "jkkjmk" };
            var exp1Compiled = exp.CompileFast();
            var exp2Compiled = (Func<InputComplex, WaitExtraData, string>)exp2.CompileFast();
            var v1 = exp1Compiled(inputComplex, extraData);
            var v2 = exp2Compiled(inputComplex, extraData);
        }


    }

    public class InputComplex
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public sealed class ExpressionSerializer : BonsaiExpressionSerializer
    {
        protected override Func<object, Json.Expression> GetConstantSerializer(Type type)
        {
            // REVIEW: Nuqleon.Json has an odd asymmetry in Serialize and Deserialize signatures,
            //         due to the inability to overload by return type. However, it seems odd we
            //         have to go serialize string and subsequently parse into Expression.

            return o => Json.Expression.Parse(new Nuqleon.Json.Serialization.JsonSerializer(type).Serialize(o), ensureTopLevelObjectOrArray: false);
        }

        protected override Func<Json.Expression, object> GetConstantDeserializer(Type type)
        {
            return json => new Nuqleon.Json.Serialization.JsonSerializer(type).Deserialize(json);
        }
    }
}
