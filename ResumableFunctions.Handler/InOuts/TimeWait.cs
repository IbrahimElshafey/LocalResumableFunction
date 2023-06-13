using System.Linq.Expressions;

namespace ResumableFunctions.Handler.InOuts;

public class TimeWait : Wait
{
    public TimeSpan TimeToWait { get; internal set; }
    internal string UniqueMatchId { get; set; }
    internal LambdaExpression SetDataExpression { get; set; }

    public Wait SetData(Expression<Func<bool>> value)
    {
        SetDataExpression = value;
        return this;
    }

}