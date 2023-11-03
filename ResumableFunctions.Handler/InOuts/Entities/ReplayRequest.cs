using System.Linq.Expressions;

namespace ResumableFunctions.Handler.InOuts.Entities;

public class ReplayRequest : WaitEntity
{
    internal ReplayRequest()
    {

    }
    public ReplayType? ReplayType { get; set; }
    internal LambdaExpression MatchExpression { get; set; }

    public override string ToString()
    {
        return $"[{nameof(ReplayType)}:{ReplayType}],[{nameof(Name)}:{Name}]";
    }
}