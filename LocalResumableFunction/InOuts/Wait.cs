﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace LocalResumableFunction.InOuts;

public abstract class Wait
{
    private MethodBase _callerMethodInfo;

    private ResumableFunctionLocal _currntFunction;

    public int Id { get; internal set; }
    public string Name { get; internal set; }
    public WaitStatus Status { get; internal set; }
    public bool IsFirst { get; internal set; }
    public int StateBeforeWait { get; internal set; }
    public int StateAfterWait { get; internal set; }
    public bool IsNode { get; internal set; }

    public WaitType WaitType { get; internal set; }

    internal ResumableFunctionState FunctionState { get; set; }

    internal int FunctionStateId { get; set; }


    /// <summary>
    ///     The resumable function that initiated requested that wait>
    ///     Will be set by handler.
    /// </summary>
    internal MethodIdentifier RequestedByFunction { get; set; }

    internal int RequestedByFunctionId { get; set; }

    /// <summary>
    ///     If not null this means that wait requested by a sub function
    ///     not
    /// </summary>
    internal Wait ParentWait { get; set; }

    internal List<Wait> ChildWaits { get; set; }

    internal int? ParentWaitId { get; set; }

    [NotMapped]
    internal ResumableFunctionLocal CurrentFunction
    {
        get
        {
            if (FunctionState is not null)
                if (FunctionState.StateObject is JObject stateAsJson)
                {
                    var type = Assembly.LoadFrom(AppContext.BaseDirectory + RequestedByFunction.AssemblyName)
                        .GetType(RequestedByFunction.ClassName);
                    var result = stateAsJson.ToObject(type);
                    FunctionState.StateObject = result;
                    _currntFunction = (ResumableFunctionLocal)result;
                    return _currntFunction;
                }
                else if (FunctionState.StateObject is ResumableFunctionLocal result)
                {
                    _currntFunction = result;
                    return _currntFunction;
                }

            return _currntFunction;
        }
        set => _currntFunction = value;
    }

    internal bool CanBeParent => this is FunctionWait || this is WaitsGroup;

    internal async Task<Wait> GetNextWait()
    {
        if (IsNode)
        {
            Console.WriteLine($"Get next wait IsNode:{IsNode},Name:{Name}");
        }
        var functionRunner = new FunctionRunner(this);
        if (functionRunner.ResumableFunctionExist is false)
        {
            Debug.WriteLine($"Resumable function ({RequestedByFunction.MethodName}) not exist in code");
            //todo:delete it and all related waits
            //throw new Exception("Can't initiate runner");
            return null;
        }

        try
        {
            var waitExist = await functionRunner.MoveNextAsync();
            if (waitExist)
            {
                Console.WriteLine($"Get next wait [{functionRunner.Current.Name}] after [{Name}]");
                return functionRunner.Current;
            }

            return null;
        }
        catch (Exception)
        {
            throw new Exception("Error when try to get next wait");
        }
    }

    public virtual bool IsFinished() => Status == WaitStatus.Completed;

    public void Cancel()
    {
        Status = WaitStatus.Canceled;
        foreach (var childWait in ChildWaits)
        {
            childWait.Status = WaitStatus.Canceled;
        }
    }

    public void CopyFromOld(Wait oldWait)
    {
        FunctionState = oldWait.FunctionState;
        FunctionStateId = oldWait.FunctionStateId;
        RequestedByFunction = oldWait.RequestedByFunction;
        RequestedByFunctionId = oldWait.RequestedByFunctionId;
    }
}