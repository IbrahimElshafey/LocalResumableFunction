using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Runtime.CompilerServices;

namespace ResumableFunctions.Handler.BaseUse
{
    public class WaitsGroup : Wait
    {
        internal WaitsGroupEntity WaitsGroupEntity { get; }

        internal WaitsGroup(WaitsGroupEntity wait) : base(wait)
        {
            WaitsGroupEntity = wait;
        }

        public int CompletedCount => WaitsGroupEntity.ChildWaits?.Count(x => x.Status == WaitStatus.Completed) ?? 0;

        public Wait MatchIf(
        Func<WaitsGroup, bool> groupMatchFilter,
        [CallerLineNumber] int inCodeLine = 0,
        [CallerMemberName] string callerName = "")
        {
            WaitsGroupEntity.WaitType = WaitType.GroupWaitWithExpression;
            WaitsGroupEntity.InCodeLine = inCodeLine;
            WaitsGroupEntity.CallerName = callerName;
            WaitsGroupEntity.GroupMatchFuncName = WaitsGroupEntity.ValidateMethod(groupMatchFilter, nameof(WaitsGroupEntity.GroupMatchFuncName));
            return this;
        }

        public Wait MatchAll()
        {
            WaitsGroupEntity.WaitType = WaitType.GroupWaitAll;
            return this;
        }

        public Wait MatchAny()
        {
            WaitsGroupEntity.WaitType = WaitType.GroupWaitFirst;
            return this;
        }

    }
}