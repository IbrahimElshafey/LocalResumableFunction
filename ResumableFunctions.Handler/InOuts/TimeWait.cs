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
               .GetMethod(nameof(LocalRegisteredMethods.TimeWait)));
    }

    public TimeSpan TimeToWait { get; internal set; }
    public bool IgnoreJobCreation { get; internal set; }
    internal string UniqueMatchId { get; set; }
    internal MethodWait TimeWaitMethod
    {
        get
        {
            _timeMethodWait.Name = Constants.TimeWaitName;
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

    public Wait AfterMatch(Action<TimeWaitInput, bool> setDataExp)
    {

        if (setDataExp != null)
        {
            _timeMethodWait.AfterMatch(setDataExp);
        }
        return this;
    }

}