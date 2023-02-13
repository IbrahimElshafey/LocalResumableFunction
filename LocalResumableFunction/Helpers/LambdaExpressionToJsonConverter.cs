using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LocalResumableFunction.Helpers;

public class LambdaExpressionToJsonConverter : ValueConverter<LambdaExpression, string>
{
    public LambdaExpressionToJsonConverter()
        : base(
            expression => ExpressionToJsonConverter.ExpressionToJson(expression),
            json => (LambdaExpression)ExpressionToJsonConverter.JsonToExpression(json))
    {
    }
}