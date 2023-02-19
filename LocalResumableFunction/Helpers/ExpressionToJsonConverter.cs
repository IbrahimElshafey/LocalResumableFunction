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
        if (expression == null) return null;
        return ExpressionToJson(expression, null);
    }

    internal static string ExpressionToJson(Expression expression, Assembly assembly = null)
    {
        if (expression != null)
            return JsonConvert.SerializeObject(expression, JsonSettings(assembly));
        return null!;
    }

    internal static Expression JsonToExpression(string json)
    {
        return JsonToExpression(json, null);
    }

    internal static Expression JsonToExpression(string json, Assembly assembly = null)
    {
        if (!string.IsNullOrWhiteSpace(json))
            return JsonConvert.DeserializeObject<LambdaExpression>(json, JsonSettings(assembly))!;
        return null!;
    }

    private static JsonSerializerSettings JsonSettings(Assembly assembly = null)
    {
        var settings = new JsonSerializerSettings();
        //Todo:Replace Assembly.GetExecutingAssembly() with "GetCurrentFunctionAssembly()"
        settings.Converters.Add(new ExpressionJsonConverter(assembly ?? Assembly.GetEntryAssembly()));
        return settings;
    }
}