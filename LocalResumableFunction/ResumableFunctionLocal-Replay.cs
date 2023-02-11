using LocalResumableFunction.InOuts;
using Newtonsoft.Json;
using LocalResumableFunction;
using System.Xml.XPath;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Runtime.CompilerServices;

public abstract partial class ResumableFunctionLocal
{

    protected Wait GoBackAfter(string name, [CallerMemberName] string callerName = "")
    {
        Wait methodToReplay = GetWait(name, callerName);
        methodToReplay.ReplayType = ReplayType.ExecuteNoWait;
        return methodToReplay;
    }
    protected Wait GoBackTo(string name, [CallerMemberName] string callerName = "")
    {
        Wait methodToReplay = GetWait(name, callerName);
        methodToReplay.ReplayType = ReplayType.WaitAgain;
        return methodToReplay;
    }

    private Wait GetWait(string name, string callerName)
    {
        var methodToReplay = FunctionState
             .Waits
             .Last(x =>
             x.Name == name &&
             x.IsNode &&
             //x.InitiatedByFunctionName == callerName &&
             x.Status == WaitStatus.Completed);
        if (methodToReplay is null)
            throw new Exception($"method go back failed, no old wait exist.");
        return methodToReplay;
    }

}
