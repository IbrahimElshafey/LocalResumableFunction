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
    internal string UniqueMatchId { get; set; }
    internal MethodWait TimeWaitMethod
    {
        get
        {
            _timeMethodWait.CurrentFunction = CurrentFunction;
            _timeMethodWait.IsNode = IsNode;
            _timeMethodWait.IsFirst = IsFirst;
            _timeMethodWait.IsNode = IsNode;
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
            _timeMethodWait.MatchIf((timeWaitInput, result) => timeWaitInput.TimeMatchId == "" && timeWaitInput.FakeProp == null);
            return _timeMethodWait;
        }
    }

    public Wait SetData(Expression<Func<bool>> value)
    {

        if (value != null)
        {
            var functionType = typeof(Func<,,>)
                .MakeGenericType(
                 typeof(TimeWaitInput),
                 typeof(bool),
                 typeof(bool));
            var inputParameter = Expression.Parameter(typeof(TimeWaitInput), "input");
            var outputParameter = Expression.Parameter(typeof(bool), "output");
            var setDataExpression = Expression.Lambda(
                functionType,
                value.Body,
                inputParameter,
                outputParameter);
            _timeMethodWait
                .SetData((Expression<Func<TimeWaitInput, bool, bool>>)setDataExpression);
        }
        return this;
    }


}