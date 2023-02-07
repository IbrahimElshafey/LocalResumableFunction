using System.Linq.Expressions;

namespace LocalResumableFunction.InOuts
{
    public class ManyMethodsWait : Wait
    {
        public List<MethodWait> WaitingMethods { get; internal set; } = new List<MethodWait>();
        public LambdaExpression? WhenCountExpression { get; internal set; }
        public MethodWait? MatchedMethod => WaitingMethods?.Single(x => x.Status == WaitStatus.Completed);

        public List<MethodWait>? MatchedMethods =>
            WaitingMethods?.Where(x => x.Status == WaitStatus.Completed).ToList();


        public Wait WhenMatchedCount(Expression<Func<int, bool>> matchCountFilter)
        {
            WhenCountExpression = matchCountFilter;
            return this;
        }

        internal void MoveToMatched(Wait currentWait)
        {
            var matchedMethod = WaitingMethods.First(x => x.Id == currentWait.Id);
            matchedMethod.Status = WaitStatus.Completed;
            CheckIsDone();
        }

        private bool CheckIsDone()
        {
            if (WhenCountExpression is null)
            {
                var required = WaitingMethods.Count(x => x.IsOptional == false);
                //MatchedMethods.Count include optional
                Status = required == MatchedMethods?.Count ? WaitStatus.Completed : Status;
            }
            else
            {
                var matchedCount = MatchedMethods?.Count ?? 0;
                var matchCompiled = (Func<int, bool>)WhenCountExpression.Compile();
                Status = matchCompiled(matchedCount) ? WaitStatus.Completed : Status;
            }
            return Status == WaitStatus.Completed;
        }
        internal void SetMatchedMethod(Wait currentWait)
        {
            WaitingMethods.ForEach(wait => wait.Status = WaitStatus.Canceled);
            var matchedMethod = WaitingMethods.First(x => x.Id == currentWait.Id);
            matchedMethod.Status = WaitStatus.Completed;
            Status = WaitStatus.Completed;
        }
    }
}