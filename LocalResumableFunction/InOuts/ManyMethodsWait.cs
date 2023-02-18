using LocalResumableFunction.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace LocalResumableFunction.InOuts;

public class ManyMethodsWait : Wait
{
    private LambdaExpression? _countExpression;
    public List<MethodWait> WaitingMethods { get; internal set; } = new();

    [NotMapped]
    public LambdaExpression? CountExpression
    {
        get => _countExpression ?? GetCountExpression();
        internal set => _countExpression = value;
    }

    private LambdaExpression GetCountExpression()
    {
        var assembly = RequestedByFunction.MethodInfo.DeclaringType.Assembly;
        return (LambdaExpression)
            ExpressionToJsonConverter.JsonToExpression(
                TextCompressor.DecompressString(CountExpressionValue), assembly);
    }

    internal byte[] CountExpressionValue { get; set; }


    public MethodWait? MatchedMethod => WaitingMethods?.Single(x => x.Status == WaitStatus.Completed);

    public List<MethodWait>? MatchedMethods =>
        WaitingMethods?.Where(x => x.Status == WaitStatus.Completed).ToList();


    public ManyMethodsWait WhenMatchedCount(Expression<Func<int, bool>> matchCountFilter)
    {
        //Debugger.Launch();
        var assembly = WaitingMethods
            .FirstOrDefault()?
            .WaitMethodIdentifier
            .MethodInfo?
            .DeclaringType?
            .Assembly;
        if (assembly != null)
        {
            CountExpression = matchCountFilter;
            CountExpressionValue =
                TextCompressor.CompressString(
                    ExpressionToJsonConverter.ExpressionToJson(CountExpression, assembly));
        }

        return this;
    }

    public Wait WaitAll()
    {
        WaitType = WaitType.AllMethodsWait;
        return this;
    }
    public Wait WaitFirst()
    {
        WaitType = WaitType.AnyMethodWait;
        return this;
    }

    internal void MoveToMatched(Wait currentWait)
    {
        var matchedMethod = WaitingMethods.First(x => x.Id == currentWait.Id);
        matchedMethod.Status = WaitStatus.Completed;
        CheckIsDone();
    }

    private void CheckIsDone()
    {
        if (CountExpression is null)
        {
            var required = WaitingMethods.Count(x => x.IsOptional == false);
            //MatchedMethods.Count include optional
            Status = required == MatchedMethods?.Count ? WaitStatus.Completed : Status;
        }
        else
        {
            var matchedCount = MatchedMethods?.Count ?? 0;
            var matchCompiled = (Func<int, bool>)CountExpression.Compile();
            Status = matchCompiled(matchedCount) ? WaitStatus.Completed : Status;
        }

        if (Status == WaitStatus.Completed)
        {
            WaitingMethods.ForEach(x =>
            {
                if (x.Status == WaitStatus.Waiting)
                    x.Status = WaitStatus.Canceled;
            });
        }
    }

    internal void SetMatchedMethod(Wait currentWait)
    {
        WaitingMethods.ForEach(wait => wait.Status = WaitStatus.Canceled);
        var matchedMethod = WaitingMethods.First(x => x.Id == currentWait.Id);
        matchedMethod.Status = WaitStatus.Completed;
        Status = WaitStatus.Completed;
    }
}