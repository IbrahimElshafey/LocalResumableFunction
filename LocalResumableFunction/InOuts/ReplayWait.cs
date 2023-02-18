using System.Linq.Expressions;

namespace LocalResumableFunction.InOuts;

public class ReplayWait : Wait
{
    public ReplayType? ReplayType { get; internal set; }
    internal LambdaExpression MatchExpression { get; set; }

    public override string ToString()
    {
        return $"[{nameof(ReplayType)}:{ReplayType}],[{nameof(Name)}:{Name}]";
    }
}