using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.Helpers.Expressions;

namespace ResumableFunctions.Handler.InOuts;

public class WaitsGroup : Wait
{
    public WaitsGroup()
    {
        WaitType = WaitType.GroupWaitAll;
    }
    private LambdaExpression _countExpression;

    [NotMapped]
    public LambdaExpression GroupMatchExpression
    {
        get => _countExpression ?? GetGroupMatchExpression();
        internal set => _countExpression = value;
    }
    internal string GroupMatchExpressionValue { get; set; }
    public int CompletedCount => ChildWaits?.Count(x => x.Status == WaitStatus.Completed) ?? 0;

    public override bool IsCompleted()
    {

        var isFinished = false;
        switch (WaitType)
        {
            case WaitType.GroupWaitAll:
                isFinished = ChildWaits?.All(x => x.Status == WaitStatus.Completed) is true;
                break;

            case WaitType.GroupWaitFirst:
                isFinished = ChildWaits?.Any(x => x.Status == WaitStatus.Completed) is true;
                break;

            case WaitType.GroupWaitWithExpression when GroupMatchExpression != null:
                var matchCompiled = (Func<WaitsGroup, bool>)GroupMatchExpression.CompileFast();
                var isCompleted = matchCompiled(this);
                Status = isCompleted ? WaitStatus.Completed : Status;
                return isCompleted;

            case WaitType.GroupWaitWithExpression:
                isFinished = ChildWaits?.Any(x => x.Status == WaitStatus.Waiting) is false;
                break;
        }
        return isFinished;
    }



    private LambdaExpression GetGroupMatchExpression()
    {
        if (GroupMatchExpressionValue != null)
        {
            var serializer = new ExpressionSerializer();
            return (LambdaExpression)serializer.Deserialize(GroupMatchExpressionValue).ToExpression();
        }
        return null;
    }

    public Wait When(Expression<Func<WaitsGroup, bool>> matchCountFilter)
    {
        WaitType = WaitType.GroupWaitWithExpression;
        var assembly = CurrentFunction?.GetType().Assembly;
        if (assembly != null)
        {
            var serializer = new ExpressionSerializer();
            GroupMatchExpression = matchCountFilter;
            GroupMatchExpressionValue = serializer.Serialize(GroupMatchExpression.ToExpressionSlim());
        }

        return this;
    }
    public Wait All()
    {
        WaitType = WaitType.GroupWaitAll;
        return this;
    }

    public Wait First()
    {
        WaitType = WaitType.GroupWaitFirst;
        return this;
    }

    internal override bool IsValidWaitRequest()
    {
        //foreach (var childWait in ChildWaits)
        //{
        //    if (!childWait.IsValidWaitRequest())
        //        break;
        //}
        return base.IsValidWaitRequest();
    }

}