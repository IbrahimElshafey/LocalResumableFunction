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

}