using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Aq.ExpressionJsonSerializer;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace ResumableFunctions.Core.Helpers;

public static class ExpressionToJsonConverter
{
    internal static string ExpressionToJson(Expression expression, Assembly assembly)
    {
        if (expression != null)
            return JsonConvert.SerializeObject(expression, JsonSettings(assembly));
        return null!;
    }

    internal static Expression JsonToExpression(string json, Assembly assembly)
    {
        if (!string.IsNullOrWhiteSpace(json))
            return JsonConvert.DeserializeObject<LambdaExpression>(json, JsonSettings(assembly))!;
        return null!;
    }

    private static JsonSerializerSettings JsonSettings(Assembly assembly)
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new ExpressionJsonConverter(assembly));
        return settings;
    }
}