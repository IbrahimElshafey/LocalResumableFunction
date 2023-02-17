using System.Linq.Expressions;

namespace LocalResumableFunction.InOuts;

public class ReplayWait : Wait
{
    public ReplayType? ReplayType { get; internal set; }
    internal LambdaExpression MatchExpression { get; set; }
}
