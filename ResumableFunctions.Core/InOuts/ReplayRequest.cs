using System.Linq.Expressions;

namespace ResumableFunctions.Core.InOuts;

public class ReplayRequest : Wait
{
    public ReplayType? ReplayType { get; internal set; }
    internal LambdaExpression MatchExpression { get; set; }

    public override string ToString()
    {
        return $"[{nameof(ReplayType)}:{ReplayType}],[{nameof(Name)}:{Name}]";
    }
}