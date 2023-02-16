using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LocalResumableFunction.InOuts;

public abstract partial class ResumableFunctionLocal
{
    protected ReplayWait GoBackAfter(string name)
    {
        return new ReplayWait { Name = name, ReplayType = ReplayType.ExecuteNoWait };
    }

    protected ReplayWait GoBackTo(string name)
    {
        return new ReplayWait { Name = name, ReplayType = ReplayType.WaitAgain };
    }

    protected ReplayWait GoBackTo<TInput, TOutput>(string name, Expression<Func<TInput, TOutput, bool>> value)
    {
        return new ReplayWait { Name = name, ReplayType = ReplayType.WaitAgainWithNewMatch };
    }
}