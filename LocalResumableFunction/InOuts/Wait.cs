using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

public abstract class Wait
{
    public MethodBase CallerInfo { get; internal set; }
    public LambdaExpression SetDataExpression { get; internal set; }
    public LambdaExpression MatchIfExpression { get; internal set; }
    public object Instance { get; internal set; }
}

public class Wait<Input, Output> : Wait
{
    public Wait<Input, Output> SetData(Expression<Action<Input, Output>> value)
    {
        SetDataExpression = value;
        return this;
    }

    public Wait<Input, Output> If(Expression<Func<Input, Output, bool>> value)
    {
        MatchIfExpression = value;
        return this;
    }
}