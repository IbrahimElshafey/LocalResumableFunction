using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Aq.ExpressionJsonSerializer;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace ResumableFunctions.Handler.Helpers;

public static class ExpressionToJsonConverter
{
    internal static string ExpressionToJson(Expression expression, Assembly assembly)
    {
        lock (_lockExpressionToJson)
        {
            if (expression != null)
                return JsonConvert.SerializeObject(expression, JsonSettings(assembly));
            return null!;
        }
    }

    private static object _lockExpressionToJson = new object();
    private static object _lockJsonToExpression = new object();
    internal static Expression JsonToExpression(string json, Assembly assembly)
    {
        lock (_lockJsonToExpression)
        {
            if (!string.IsNullOrWhiteSpace(json))
                return JsonConvert.DeserializeObject<LambdaExpression>(json, JsonSettings(assembly))!;
            return null!;
        }
    }

    private static JsonSerializerSettings JsonSettings(Assembly assembly)
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new ExpressionJsonConverter(assembly));
        return settings;
    }
}