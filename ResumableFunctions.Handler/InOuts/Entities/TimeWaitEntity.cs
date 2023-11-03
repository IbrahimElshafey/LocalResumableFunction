using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler.InOuts.Entities;

public class TimeWaitEntity : WaitEntity
{
    internal readonly MethodWaitEntity<TimeWaitInput, bool> _timeMethodWait;
    internal TimeWaitEntity()
    {

    }
    internal TimeWaitEntity(ResumableFunctionsContainer currentFunction)
    {
        var timeWaitMethod = typeof(LocalRegisteredMethods)
                        .GetMethod(nameof(LocalRegisteredMethods.TimeWait));

        _timeMethodWait =
            new MethodWaitEntity<TimeWaitInput, bool>(timeWaitMethod) { CurrentFunction = currentFunction };
    }

    public TimeSpan TimeToWait { get; set; }
    public bool IgnoreJobCreation { get; set; }
    internal string UniqueMatchId { get; set; }
    internal MethodWaitEntity TimeWaitMethod
    {
        get
        {
            _timeMethodWait.Name = Constants.TimeWaitName;
            _timeMethodWait.CurrentFunction = CurrentFunction;
            _timeMethodWait.IsFirst = IsFirst;
            _timeMethodWait.WasFirst = WasFirst;
            _timeMethodWait.IsRoot = IsRoot;
            _timeMethodWait.ParentWait = ParentWait;
            _timeMethodWait.FunctionState = FunctionState;
            _timeMethodWait.RequestedByFunctionId = RequestedByFunctionId;
            _timeMethodWait.StateBeforeWait = StateBeforeWait;
            _timeMethodWait.StateAfterWait = StateAfterWait;
            _timeMethodWait.Locals = Locals;
            _timeMethodWait.CallerName = CallerName;
            _timeMethodWait.InCodeLine = InCodeLine;
            _timeMethodWait.ExtraData =
                new WaitExtraData
                {
                    TimeToWait = TimeToWait,
                    UniqueMatchId = UniqueMatchId,
                };
            _timeMethodWait.MatchIf((timeWaitInput, result) => timeWaitInput.TimeMatchId == string.Empty);
            return _timeMethodWait;
        }
    }

    internal TimeWait ToTimeWait() => new TimeWait(this);
}