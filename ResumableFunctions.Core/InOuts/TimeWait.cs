using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace ResumableFunctions.Core.InOuts;

public class TimeWait : Wait
{
    public TimeSpan TimeToWait { get; internal set; }
    public string UniqueMatchId { get; internal set; }
    public LambdaExpression SetDataExpression { get; internal set; }

    public Wait SetData(Expression<Func<bool>> value)
    {
        SetDataExpression = value;
        return this;
    }

}