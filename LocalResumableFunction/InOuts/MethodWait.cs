using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using LocalResumableFunction.Data;
using LocalResumableFunction.Helpers;

namespace LocalResumableFunction.InOuts;
public class MethodWait : Wait
{
    public ManyMethodsWait ParentWaitsGroup { get; internal set; }

    public int? ParentWaitsGroupId { get; internal set; }

    public bool IsOptional { get; internal set; }

    [NotMapped]
    public LambdaExpression SetDataExpression { get; protected set; }

    internal byte[] SetDataExpressionValue { get; set; }

    [NotMapped]
    public LambdaExpression MatchIfExpression { get; protected set; }

    internal byte[] MatchIfExpressionValue { get; set; }

    public bool NeedFunctionStateForMatch { get; internal set; } = false;


    /// <summary>
    /// The method that we wait to resume resumable function
    /// </summary>
    internal MethodIdentifier WaitMethodIdentifier { get; set; }

    internal int WaitMethodIdentifierId { get; set; }

    internal void SetExpressions()
    {
        var assembly = WaitMethodIdentifier.MethodInfo.DeclaringType.Assembly;
        SetMatchExpression(MatchIfExpression);
        SetDataExpression = new RewriteSetDataExpression(this).Result;
        SetDataExpressionValue = TextCompressor.CompressString(ExpressionToJsonConverter.ExpressionToJson(SetDataExpression, assembly));
    }

    internal void SetMatchExpression(LambdaExpression matchExpression)
    {
        var assembly = WaitMethodIdentifier.MethodInfo.DeclaringType.Assembly;
        MatchIfExpression = matchExpression;
        MatchIfExpression = new RewriteMatchExpression(this).Result;
        MatchIfExpressionValue =
            TextCompressor.CompressString(ExpressionToJsonConverter.ExpressionToJson(MatchIfExpression, assembly));
    }

    internal void LoadExpressions()
    {
        var assembly = WaitMethodIdentifier.MethodInfo.DeclaringType.Assembly;
        MatchIfExpression = (LambdaExpression)
            ExpressionToJsonConverter.JsonToExpression(
                TextCompressor.DecompressString(MatchIfExpressionValue), assembly);
        SetDataExpression = (LambdaExpression)
            ExpressionToJsonConverter.JsonToExpression(
                TextCompressor.DecompressString(SetDataExpressionValue), assembly);
    }
}

public class MethodWait<TInput, TOutput> : MethodWait
{
    public MethodWait(Func<TInput, TOutput> method)
    {
        var eventMethodAttributeExist = method.Method.GetCustomAttribute(typeof(WaitMethodAttribute));
        if (eventMethodAttributeExist == null)
            throw new Exception(
                $"You must add attribute [{nameof(WaitMethodAttribute)}] to method {method.Method.Name}");

        WaitMethodIdentifier = new MethodIdentifier();
        WaitMethodIdentifier.SetMethodInfo(method.Method);
        Name = "#NoName#";
    }

    public MethodWait<TInput, TOutput> SetData(Expression<Func<TInput, TOutput, bool>> value)
    {
        SetDataExpression = value;
        return this;
    }

    public MethodWait<TInput, TOutput> If(Expression<Func<TInput, TOutput, bool>> value)
    {
        MatchIfExpression = value;
        return this;
    }

    public MethodWait<TInput, TOutput> SetOptional()
    {
        IsOptional = true;
        return this;
    }
}