using System.Linq.Expressions;
using System.Reflection;
using LocalResumableFunction.Data;
using LocalResumableFunction.Helpers;

namespace LocalResumableFunction.InOuts;

public class ReplayWait : Wait
{
    public ReplayType? ReplayType { get; internal set; }
}
public class MethodWait : Wait
{
    public ManyMethodsWait ParentWaitsGroup { get; internal set; }

    public int? ParentWaitsGroupId { get; internal set; }

    public bool IsOptional { get; internal set; }

    public LambdaExpression SetDataExpression { get; internal set; }
    public LambdaExpression MatchIfExpression { get; internal set; }
    public bool NeedFunctionStateForMatch { get; internal set; } = false;

    /// <summary>
    /// The method that we wait to resume resumable function
    /// </summary>
    internal MethodIdentifier WaitMethodIdentifier { get; set; }

    internal int WaitMethodIdentifierId { get; set; }
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
        WaitMethodIdentifier.SetMethodBase(method.Method);
    }

    public MethodWait<TInput, TOutput> SetData(Expression<Action<TInput, TOutput>> value)
    {
        SetDataExpression = value;
        SetDataExpression = new RewriteSetDataExpression(this).Result;
        return this;
    }

    public MethodWait<TInput, TOutput> If(Expression<Func<TInput, TOutput, bool>> value)
    {
        MatchIfExpression = value;
        MatchIfExpression = new RewriteMatchExpression(this).Result;
        return this;
    }

    public MethodWait<TInput, TOutput> SetOptional()
    {
        IsOptional = true;
        return this;
    }
}