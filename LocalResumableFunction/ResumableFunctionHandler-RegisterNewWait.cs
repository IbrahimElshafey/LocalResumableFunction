using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal partial class ResumableFunctionHandler
{
    internal async Task<bool> GenericWaitRequested(Wait newWait)
    {
        newWait.Status = WaitStatus.Waiting;
        if (Validate(newWait) is false) return false;
        switch (newWait)
        {
            case MethodWait methodWait:
                await SingleWaitRequested(methodWait);
                break;
            case WaitsGroup manyWaits:
                await ManyWaitsRequested(manyWaits);
                break;
            case FunctionWait functionWait:
                await FunctionWaitRequested(functionWait);
                break;
            //case ManyFunctionsWait manyFunctionsWait:
            //    await ManyFunctionsWaitRequested(manyFunctionsWait);
            //    break;
        }

        return false;
    }

    private async Task SingleWaitRequested(MethodWait methodWait)
    {
        var waitMethodIdentifier = await _metodIdsRepo.GetMethodIdentifier(methodWait.WaitMethodIdentifier);
        methodWait.WaitMethodIdentifier = waitMethodIdentifier;
        methodWait.WaitMethodIdentifierId = waitMethodIdentifier.Id;
        methodWait.SetExpressions();

        await _waitsRepository.AddWait(methodWait);
    }

    private async Task ManyWaitsRequested(WaitsGroup manyWaits)
    {
        for (var index = 0; index < manyWaits.ChildWaits.Count; index++)
        {
            var wait = manyWaits.ChildWaits[index];
            wait.Status = WaitStatus.Waiting;
            wait.FunctionState = manyWaits.FunctionState;
            wait.RequestedByFunctionId = manyWaits.RequestedByFunctionId;
            wait.StateAfterWait = manyWaits.StateAfterWait;
            wait.ParentWaitId = manyWaits.ParentWaitId;
            await GenericWaitRequested(wait);
        }

        await _waitsRepository.AddWait(manyWaits);
    }

    //private async Task ManyFunctionsWaitRequested(ManyFunctionsWait manyFunctionsWaits)
    //{
    //    foreach (var functionWait in manyFunctionsWaits.WaitingFunctions)
    //    {
    //        functionWait.Status = WaitStatus.Waiting;
    //        functionWait.FunctionState = manyFunctionsWaits.FunctionState;
    //        functionWait.RequestedByFunctionId = manyFunctionsWaits.RequestedByFunctionId;
    //        functionWait.StateAfterWait = manyFunctionsWaits.StateAfterWait;
    //        functionWait.ParentWaitId = manyFunctionsWaits.ParentWaitId;
    //    }

    //    await _waitsRepository.AddWait(manyFunctionsWaits);
    //    await _context.SaveChangesAsync();

    //    foreach (var functionWait in manyFunctionsWaits.WaitingFunctions)
    //    {
    //        try
    //        {
    //            await FunctionWaitRequested(functionWait);
    //            await _context.SaveChangesAsync();
    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e);
    //            throw;
    //        }
    //    }
    //}

    private async Task FunctionWaitRequested(FunctionWait functionWait)
    {
        await _waitsRepository.AddWait(functionWait);
        //await _context.SaveChangesAsync();

        var functionRunner = new FunctionRunner(functionWait.CurrentFunction, functionWait.FunctionInfo);
        var hasNext = await functionRunner.MoveNextAsync();
        functionWait.FirstWait = functionRunner.Current;
        if (hasNext is false)
        {
            WriteMessage($"No waits exist in sub function ({functionWait.FunctionInfo.Name})");
            return;
        }

        functionWait.FirstWait = functionRunner.Current;
        //functionWait.FirstWait.StateAfterWait = functionRunner.GetState();
        functionWait.FirstWait.FunctionState = functionWait.FunctionState;
        functionWait.FirstWait.FunctionStateId = functionWait.FunctionState.Id;
        functionWait.FirstWait.ParentWait = functionWait;
        functionWait.FirstWait.ParentWaitId = functionWait.Id;
        var methodIdentifier = await _metodIdsRepo.GetMethodIdentifier(functionWait.FunctionInfo);
        functionWait.FirstWait.RequestedByFunction = methodIdentifier.MethodIdentifier;
        functionWait.FirstWait.RequestedByFunctionId = methodIdentifier.MethodIdentifier.Id;

        if (functionWait.FirstWait is ReplayWait replayWait)
            await ReplayWait(replayWait);//todo:review first wait is replay for what??
        else
            await GenericWaitRequested(functionWait.FirstWait);
    }

    

    private bool Validate(Wait nextWait)
    {
        return true;
    }
}