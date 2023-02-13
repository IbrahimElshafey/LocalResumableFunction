using System.Linq.Expressions;
using System.Reflection;
using Aq.ExpressionJsonSerializer;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace LocalResumableFunction.Helpers;

public class ExpressionToJsonConverter : ValueConverter<Expression, string>
{
    public ExpressionToJsonConverter()
        : base(
            expression => ExpressionToJson(expression),
            json => JsonToExpression(json))
    {
    }

    internal static string ExpressionToJson(Expression expression)
    {
        if (expression != null)
            return JsonConvert.SerializeObject(expression, JsonSettings());
        return null!;
    }

    internal static Expression JsonToExpression(string json)
    {
        if (!string.IsNullOrWhiteSpace(json))
            return JsonConvert.DeserializeObject<LambdaExpression>(json, JsonSettings())!;
        return null!;
    }

    private static JsonSerializerSettings JsonSettings()
    {
        var settings = new JsonSerializerSettings();
        //Todo:Replace Assembly.GetExecutingAssembly() with "GetCurrentFunctionAssembly()"
        settings.Converters.Add(
            new ExpressionJsonConverter(Assembly.GetEntryAssembly()));
        return settings;
    }
}