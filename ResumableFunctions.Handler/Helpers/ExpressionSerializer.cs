﻿using System.Linq.Expressions.Bonsai.Serialization;
using Json = Nuqleon.Json.Expressions;
namespace ResumableFunctions.Handler.Helpers
{
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