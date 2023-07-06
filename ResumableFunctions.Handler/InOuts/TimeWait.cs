using ResumableFunctions.Handler.Helpers;
using System.Linq.Expressions;

namespace ResumableFunctions.Handler.InOuts;

public class TimeWait : Wait
{
    private readonly MethodWait<TimeWaitInput, bool> _timeMethodWait;

    internal TimeWait()
    {
        _timeMethodWait =
           new MethodWait<TimeWaitInput, bool>(typeof(LocalRegisteredMethods)
               .GetMethod("TimeWait"));
    }

    public TimeSpan TimeToWait { get; internal set; }
    public bool IgnoreJobCreation { get; internal set; }
    internal string UniqueMatchId { get; set; }
    internal MethodWait TimeWaitMethod
    {
        get
        {
            _timeMethodWait.Name = $"#{nameof(LocalRegisteredMethods.TimeWait)}#";
            _timeMethodWait.CurrentFunction = CurrentFunction;
            _timeMethodWait.IsFirst = IsFirst;
            _timeMethodWait.WasFirst = WasFirst;
            _timeMethodWait.IsRootNode = IsRootNode;
            _timeMethodWait.ParentWait = ParentWait;
            _timeMethodWait.FunctionState = FunctionState;
            _timeMethodWait.RequestedByFunctionId = RequestedByFunctionId;
            _timeMethodWait.StateBeforeWait = StateBeforeWait;
            _timeMethodWait.StateAfterWait = StateAfterWait;
            _timeMethodWait.ExtraData =
                new WaitExtraData
                {
                    TimeToWait = TimeToWait,
                    UniqueMatchId = UniqueMatchId,
                };
            _timeMethodWait.MatchIf((timeWaitInput, result) => timeWaitInput.TimeMatchId == "");
            return _timeMethodWait;
        }
    }

    public Wait SetData(Expression<Func<TimeWaitInput, bool>> setDataExp)
    {

        if (setDataExp != null)
        {
            var functionType = typeof(Func<,,>)
                .MakeGenericType(
                 typeof(TimeWaitInput),
                 typeof(bool),
                 typeof(bool));
            var inputParameter = setDataExp.Parameters[0];
            var outputParameter = Expression.Parameter(typeof(bool), "output");
            var setDataExpression = Expression.Lambda(
                functionType,
                setDataExp.Body,
                inputParameter,
                outputParameter);
            _timeMethodWait
                .SetData((Expression<Func<TimeWaitInput, bool, bool>>)setDataExpression);
        }
        return this;
    }


}