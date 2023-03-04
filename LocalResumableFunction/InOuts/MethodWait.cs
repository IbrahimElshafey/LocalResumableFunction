using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using LocalResumableFunction.Helpers;

namespace LocalResumableFunction.InOuts;

public class MethodWait : Wait
{

    [NotMapped] public LambdaExpression SetDataExpression { get; internal set; }

    internal byte[] SetDataExpressionValue { get; set; }

    [NotMapped]
    public LambdaExpression MatchIfExpression { get; internal set; }

    internal byte[] MatchIfExpressionValue { get; set; }

    public bool NeedFunctionStateForMatch { get; internal set; } = false;


    /// <summary>
    ///     The method that we wait to resume resumable function
    /// </summary>
    internal MethodIdentifier WaitMethodIdentifier { get; set; }

    [NotMapped]
    internal MethodData MethodData { get; set; }

    internal int WaitMethodIdentifierId { get; set; }

    [NotMapped]
    public object Input { get; set; }

    [NotMapped]
    public object Output { get; set; }

    private Assembly FunctionAssembly => 
        RequestedByFunction?.MethodInfo.DeclaringType.Assembly??
        WaitMethodIdentifier?.MethodInfo.DeclaringType.Assembly;

    internal void SetExpressions()
    {
        SetMatchExpression(MatchIfExpression);
        SetDataExpression = new RewriteSetDataExpression(this).Result;
        SetDataExpressionValue =
            TextCompressor.CompressString(ExpressionToJsonConverter.ExpressionToJson(SetDataExpression, FunctionAssembly));
    }

    internal void SetMatchExpression(LambdaExpression matchExpression)
    {
        MatchIfExpression = matchExpression;
        MatchIfExpression = new RewriteMatchExpression(this).Result;
        MatchIfExpressionValue =
            TextCompressor.CompressString(ExpressionToJsonConverter.ExpressionToJson(MatchIfExpression, FunctionAssembly));
    }

    internal void LoadExpressions()
    {
        MatchIfExpression = (LambdaExpression)
            ExpressionToJsonConverter.JsonToExpression(
                TextCompressor.DecompressString(MatchIfExpressionValue), FunctionAssembly);
        SetDataExpression = (LambdaExpression)
            ExpressionToJsonConverter.JsonToExpression(
                TextCompressor.DecompressString(SetDataExpressionValue), FunctionAssembly);
    }

    public void UpdateFunctionData()
    {
        var setDataExpression = SetDataExpression.Compile();
        setDataExpression.DynamicInvoke(Input, Output, CurrentFunction);
        FunctionState.StateObject = CurrentFunction;
    }

    public bool CheckMatch()
    {
        try
        {
            if (IsFirst && MatchIfExpressionValue == null)
                return true;
            var check = MatchIfExpression.Compile();
            return (bool)check.DynamicInvoke(Input, Output, CurrentFunction);
        }
        catch (Exception e)
        {
            return false;
        }
    }
}

public class MethodWait<TInput, TOutput> : MethodWait
{
    public MethodWait(Func<TInput, Task<TOutput>> method)
    {
        Create(method.Method);
    }
    public MethodWait(Func<TInput, TOutput> method)
    {
        Create(method.Method);
    }

    private void Create(MethodInfo method)
    {
        var eventMethodAttributeExist = method.GetCustomAttribute(typeof(WaitMethodAttribute));
        if (eventMethodAttributeExist == null)
            throw new Exception(
                $"You must add attribute [{nameof(WaitMethodAttribute)}] to method {method.Name}");

        MethodData = new MethodData(method);
        Name = $"#{method.Name}#";
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

}