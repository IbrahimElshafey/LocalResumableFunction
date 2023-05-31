﻿using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler.InOuts;

public abstract class Wait : IEntityWithUpdate, IEntityWithDelete
{
    public int Id { get; internal set; }
    public DateTime Created { get; internal set; }
    public string Name { get; internal set; }
    public WaitStatus Status { get; internal set; } = WaitStatus.Waiting;
    public bool IsFirst { get; internal set; }
    public int StateBeforeWait { get; internal set; }
    public int StateAfterWait { get; internal set; }
    public bool IsNode { get; internal set; }
    public bool IsReplay { get; internal set; }
    public object ExtraData { get; internal set; }

    public WaitType WaitType { get; internal set; }
    public DateTime Modified { get; internal set; }
    public string ConcurrencyToken { get; internal set; }
    public bool IsDeleted { get; internal set; }

    internal ResumableFunctionState FunctionState { get; set; }

    internal int FunctionStateId { get; set; }

    internal ServiceData Service { get; set; }

    internal int? ServiceId { get; set; }


    /// <summary>
    ///     The resumable function that initiated/created/requested the wait.
    /// </summary>
    internal ResumableFunctionIdentifier RequestedByFunction { get; set; }

    internal int RequestedByFunctionId { get; set; }

    /// <summary>
    ///     If not null this means that wait requested by a sub function
    ///     not
    /// </summary>
    internal Wait ParentWait { get; set; }

    internal List<Wait> ChildWaits { get; set; } = new();

    internal int? ParentWaitId { get; set; }
    public string Path { get; internal set; }

    private ResumableFunction _currentFunction;
    [NotMapped]
    internal ResumableFunction CurrentFunction
    {
        get
        {
            if (
                FunctionState is not null &&
                FunctionState.StateObject is JObject stateAsJson)
            {
                var type = Assembly.LoadFrom(AppContext.BaseDirectory + RequestedByFunction.AssemblyName)
                    .GetType(RequestedByFunction.ClassName);

                var result = (ResumableFunction)stateAsJson.ToObject(type);

                FunctionState.StateObject = result;
                _currentFunction = result;
            }

            return (ResumableFunction)FunctionState?.StateObject ?? _currentFunction;
        }
        set
        {
            _currentFunction = value;
            if (FunctionState is not null)
                FunctionState.StateObject = value;
        }
    }

    internal bool CanBeParent => this is FunctionWait || this is WaitsGroup;


    internal async Task<Wait> GetNextWait()
    {

        var functionRunner = new FunctionRunner(this);
        if (functionRunner.ResumableFunctionExistInCode is false)
        {
            var errorMsg = $"Resumable function ({RequestedByFunction.MethodName}) not exist in code";
            FunctionState.AddError(errorMsg);
            throw new Exception(errorMsg);
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
        catch (Exception ex)
        {
            FunctionState.AddError(
                $"An error occurred after resuming execution after wait `{this}`.", ex);
            FunctionState.Status = FunctionStatus.Error;
            throw;
        }
        finally
        {
            CurrentFunction.Logs.ForEach(log =>
            {
                log.IsCustom = true;
                log.EntityType = nameof(ResumableFunctionState);
            });
            FunctionState.Logs.AddRange(CurrentFunction.Logs);
            FunctionState.Status =
              CurrentFunction.HasError || FunctionState.HasError ?
              FunctionStatus.Error :
              FunctionStatus.InProgress;
        }
    }

    public virtual bool IsCompleted() => Status == WaitStatus.Completed;



    public void CopyFromOld(Wait oldWait)
    {
        FunctionState = oldWait.FunctionState;
        FunctionStateId = oldWait.FunctionStateId;
        RequestedByFunction = oldWait.RequestedByFunction;
        RequestedByFunctionId = oldWait.RequestedByFunctionId;
    }

    public Wait DuplicateWait()
    {
        Wait result;
        switch (this)
        {
            case MethodWait methodWait:
                result = new MethodWait();
                result.CopyMethod(methodWait, (MethodWait)result);
                break;
            case FunctionWait:
                result = new FunctionWait();
                break;
            case WaitsGroup waitsGroup:
                result = new WaitsGroup
                {
                    GroupMatchExpressionValue = waitsGroup.GroupMatchExpressionValue
                };
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        result.CopyCommon(this);
        CopyChildTree(this, result);
        return result;
    }
    private void CopyChildTree(Wait fromWait, Wait toWait)
    {
        for (var index = 0; index < fromWait.ChildWaits.Count; index++)
        {
            var childWait = fromWait.ChildWaits[index];
            var duplicateWait = childWait.DuplicateWait();
            toWait.ChildWaits.Add(duplicateWait);
            if (childWait.CanBeParent)
                CopyChildTree(childWait, duplicateWait);
        }
    }
    private void CopyMethod(MethodWait from, MethodWait to)
    {
        to.SetDataExpressionValue = from.SetDataExpressionValue;
        to.MatchIfExpressionValue = from.MatchIfExpressionValue;
        to.MethodToWaitId = from.MethodToWaitId;
        to.MethodToWait = from.MethodToWait;
        to.LoadExpressions();
    }

    private void CopyCommon(Wait fromWait)
    {
        Name = fromWait.Name;
        Status = fromWait.Status;
        IsFirst = fromWait.IsFirst;
        StateBeforeWait = fromWait.StateBeforeWait;
        StateAfterWait = fromWait.StateAfterWait;
        IsNode = fromWait.IsNode;
        IsReplay = fromWait.IsReplay;
        ExtraData = fromWait.ExtraData;
        WaitType = fromWait.WaitType;
        FunctionStateId = fromWait.FunctionStateId;
        FunctionState = fromWait.FunctionState;
        ParentWaitId = fromWait.ParentWaitId;
        RequestedByFunctionId = fromWait.RequestedByFunctionId;
        RequestedByFunction = fromWait.RequestedByFunction;
    }

    internal virtual void Cancel() => Status = Status == WaitStatus.Waiting ? Status = WaitStatus.Canceled : Status;

    internal virtual bool IsValidWaitRequest()
    {
        //FunctionState.StatusMessage = message;
        //FunctionState.Status = FunctionStatus.ErrorOccured;
        var isNameDuplicated =
            FunctionState
            .Waits
            .Count(x => x.Name == Name) > 1;
        if (isNameDuplicated)
        {
            FunctionState.AddLog(
                $"The wait named [{Name}] is duplicated in function body,fix it to not cause a problem. If it's a loop concat the  index to the name",
                LogType.Warning);
        }
        return FunctionState.HasError is false;
    }


    internal void ActionOnWaitsTree(Action<Wait> action)
    {
        action(this);
        if (ChildWaits != null)
            foreach (var item in ChildWaits)
                item.ActionOnWaitsTree(action);
    }

    internal MethodWait GetChildMethodWait(string name)
    {
        var result = this
            .Flatten(x => x.ChildWaits)
            .FirstOrDefault(x => x.Name == name && x is MethodWait mw);
        if (result == null)
            throw new NullReferenceException($"No MethodWait with name [{name}] exist in ChildWaits tree [{Name}]");
        return (MethodWait)result;
    }

    public override string ToString()
    {
        return $"Name:{Name}, Type:{WaitType}, Id:{Id}";
    }
}