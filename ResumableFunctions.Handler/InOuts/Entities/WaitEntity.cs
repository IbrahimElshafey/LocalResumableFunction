using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ResumableFunctions.Handler.InOuts.Entities;

public abstract class WaitEntity : IEntity<long>, IEntityWithUpdate, IEntityWithDelete, IOnSaveEntity
{

    public long Id { get; set; }
    public DateTime Created { get; set; }
    public string Name { get; set; }
    public WaitStatus Status { get; set; } = WaitStatus.Waiting;
    public bool IsFirst { get; set; }
    public bool WasFirst { get; set; }
    public int StateBeforeWait { get; set; }
    public int StateAfterWait { get; set; }
    public bool IsRoot { get; set; }
    public bool IsReplay { get; set; }

    [NotMapped]
    public WaitExtraData ExtraData { get; set; }
    public byte[] ExtraDataValue { get; set; }

    public int? ServiceId { get; set; }

    public WaitType WaitType { get; set; }
    public DateTime Modified { get; set; }
    public string ConcurrencyToken { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The state object of current resumable function container.
    /// </summary>
    internal ResumableFunctionState FunctionState { get; set; }

    internal int FunctionStateId { get; set; }


    /// <summary>
    ///  The resumable function that initiated/created/requested the wait.
    ///  May be resumable function or sub resumable function.
    /// </summary>
    internal ResumableFunctionIdentifier RequestedByFunction { get; set; }

    internal int RequestedByFunctionId { get; set; }

    /// <summary>
    ///     If not null this means that wait requested by a sub function
    ///     not
    /// </summary>
    internal WaitEntity ParentWait { get; set; }
    internal long? ParentWaitId { get; set; }

    internal List<WaitEntity> ChildWaits { get; set; } = new();

    /// <summary>
    /// Local variables in method at the wait point where current wait requested
    /// It's the runner class serialized we can rename this to RunnerState
    /// </summary>
    public object Locals { get; private set; }



    public RuntimeClosure RuntimeClosure { get; set; }

    [NotMapped]
    public WaitEntity OldCompletedSibling { get; set; }

    public Guid? RuntimeClosureId { get; set; }

    /// <summary>
    /// Local variables that is closed (make a closure) in match expression or callbacks.
    /// </summary>
    public object ImmutableClosure { get; internal set; }

    public string Path { get; set; }

    [NotMapped]
    internal ResumableFunctionsContainer CurrentFunction { get; set; }

    internal bool CanBeParent => this is FunctionWaitEntity || this is WaitsGroupEntity;
    internal long? CallId { get; set; }
    public int InCodeLine { get; set; }
    public string CallerName { get; set; }




    //MethodWait.AfterMatch(Action<TInput, TOutput>)
    //MethodWait.WhenCancel(Action cancelAction)
    //WaitsGroup.MatchIf(Func<WaitsGroup, bool>)
    //The method may update closure  
    protected object CallMethodByName(string methodFullName, params object[] parameters)
    {
        var parts = methodFullName.Split('#');
        var methodName = parts[1];
        var className = parts[0];//may be the RFContainer calss or closure class

        //is local method in current function class
        object rfClassInstance = CurrentFunction;
        var rfClassType = rfClassInstance.GetType();
        var localMethodInfo = rfClassType.GetMethod(methodName, Flags());
        if (localMethodInfo != null)
            return localMethodInfo.Invoke(rfClassInstance, parameters);

        //is lambda method (closure exist)
        var closureType = rfClassType.Assembly.GetType(className);
        if (closureType != null)
        {
            var closureMethodInfo = closureType.GetMethod(methodName, Flags());
            var closureInstance = RuntimeClosure?.AsType(closureType);
            SetClosureCaller(closureInstance);

            if (closureMethodInfo != null)
            {
                var result = closureMethodInfo.Invoke(closureInstance, parameters);
                RuntimeClosure.Value = closureInstance;
                return result;
            }
        }

        throw new NullReferenceException(
            $"Can't find method [{methodName}] in class [{rfClassType.Name}]");
    }

    private void SetClosureCaller(object closureInstance)
    {
        if (closureInstance == null) return;

        var closureType = closureInstance.GetType();
        bool notClosureClass = !closureType.Name.StartsWith(Constants.CompilerClosurePrefix);
        if (notClosureClass) return;

        var thisField = closureType
            .GetFields()
            .FirstOrDefault(x => x.Name.EndsWith(Constants.CompilerCallerSuffix) && x.FieldType == CurrentFunction.GetType());
        if (thisField != null)
        {
            thisField.SetValue(closureInstance, CurrentFunction);
        }
        // may be multiple closures in same IAsyncEnumrable where clsoure C1 is field in closure C2 and so on.
        else
        {
            var parentClosuresFields = closureType
                .GetFields()
                .Where(x => x.FieldType.Name.StartsWith(Constants.CompilerClosurePrefix));
            foreach (var closureField in parentClosuresFields)
            {
                SetClosureCaller(closureField.GetValue(closureInstance));
            }
        }
    }

    internal async Task<WaitEntity> GetNextWait()
    {
        if (CurrentFunction == null)
            LoadUnmappedProps();
        var functionRunner = new FunctionRunner(this);
        if (functionRunner.ResumableFunctionExistInCode is false)
        {
            var errorMsg = $"Resumable function ({RequestedByFunction.MethodName}) not exist in code";
            FunctionState.AddError(errorMsg, StatusCodes.MethodValidation, null);
            throw new Exception(errorMsg);
        }

        try
        {
            var waitExist = await functionRunner.MoveNextAsync();
            if (waitExist)
            {
                var nextWait = functionRunner.CurrentWait;
                var replaySuffix = nextWait is ReplayRequest ? " - Replay" : "";

                FunctionState.AddLog(
                    $"Get next wait [{functionRunner.CurrentWait.Name}{replaySuffix}] " +
                    $"after [{Name}]", LogType.Info, StatusCodes.WaitProcessing);

                nextWait.ParentWaitId = ParentWaitId;
                FunctionState.StateObject = CurrentFunction;
                nextWait.FunctionState = FunctionState;
                nextWait.RequestedByFunctionId = RequestedByFunctionId;

                return nextWait = functionRunner.CurrentWait;
            }

            return null;
        }
        catch (Exception ex)
        {
            FunctionState.AddError(
                $"An error occurred after resuming execution after wait [{this}].", StatusCodes.WaitProcessing, ex);
            FunctionState.Status = FunctionInstanceStatus.InError;
            throw;
        }
        finally
        {
            CurrentFunction.Logs.ForEach(log => log.EntityType = nameof(ResumableFunctionState));
            FunctionState.Logs.AddRange(CurrentFunction.Logs);
            FunctionState.Status =
              CurrentFunction.HasErrors() || FunctionState.HasErrors() ?
              FunctionInstanceStatus.InError :
              FunctionInstanceStatus.InProgress;
        }
    }

    internal virtual bool IsCompleted() => Status == WaitStatus.Completed;


    public virtual void CopyCommonIds(WaitEntity oldWait)
    {
        FunctionState = oldWait.FunctionState;
        FunctionStateId = oldWait.FunctionStateId;
        RequestedByFunction = oldWait.RequestedByFunction;
        RequestedByFunctionId = oldWait.RequestedByFunctionId;
    }

    public WaitEntity DuplicateWait()
    {
        WaitEntity result;
        switch (this)
        {
            case MethodWaitEntity methodWait:
                result = new MethodWaitEntity
                {
                    TemplateId = methodWait.TemplateId,
                    MethodGroupToWaitId = methodWait.MethodGroupToWaitId,
                    MethodToWaitId = methodWait.MethodToWaitId,
                    ImmutableClosure = methodWait.ImmutableClosure,
                };
                break;
            case FunctionWaitEntity:
                result = new FunctionWaitEntity();
                break;
            case WaitsGroupEntity waitsGroup:
                result = new WaitsGroupEntity
                {
                    GroupMatchFuncName = waitsGroup.GroupMatchFuncName
                };
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        result.CopyCommon(this);
        CopyChildTree(this, result);
        return result;
    }
    private void CopyChildTree(WaitEntity fromWait, WaitEntity toWait)
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

    private void CopyCommon(WaitEntity fromWait)
    {
        Name = fromWait.Name;
        Status = fromWait.Status;
        IsFirst = fromWait.IsFirst;
        StateBeforeWait = fromWait.StateBeforeWait;
        StateAfterWait = fromWait.StateAfterWait;
        Locals = fromWait.Locals;
        IsRoot = fromWait.IsRoot;
        IsReplay = fromWait.IsReplay;
        ExtraData = fromWait.ExtraData;
        WaitType = fromWait.WaitType;
        FunctionStateId = fromWait.FunctionStateId;
        FunctionState = fromWait.FunctionState;
        ParentWaitId = fromWait.ParentWaitId;
        RequestedByFunctionId = fromWait.RequestedByFunctionId;
        RequestedByFunction = fromWait.RequestedByFunction;
        CallerName = fromWait.CallerName;
    }

    internal virtual void Cancel() => Status = Status == WaitStatus.Waiting ? Status = WaitStatus.Canceled : Status;

    internal virtual bool ValidateWaitRequest()
    {
        var isNameDuplicated =
            FunctionState
            .Waits
            .Count(x => x.Name == Name) > 1;
        if (isNameDuplicated)
        {
            FunctionState.AddLog(
                $"The wait named [{Name}] is duplicated in function [{RequestedByFunction?.MethodName}] body,fix it to not cause a problem. If it's a loop concat the  index to the name",
                LogType.Warning, StatusCodes.WaitValidation);
        }

        var hasErrors = FunctionState.HasErrors();
        if (hasErrors)
        {
            Status = WaitStatus.InError;
            FunctionState.Status = FunctionInstanceStatus.InError;
        }
        return hasErrors is false;
    }


    /// <summary>
    /// Including the current one
    /// </summary>
    internal void ActionOnParentTree(Action<WaitEntity> action)
    {
        action(this);
        if (ParentWait != null)
            ParentWait.ActionOnParentTree(action);
    }

    /// <summary>
    /// Including the current one
    /// </summary>
    internal void ActionOnChildrenTree(Action<WaitEntity> action)
    {
        action(this);
        if (ChildWaits != null)
            foreach (var item in ChildWaits)
                item.ActionOnChildrenTree(action);
    }

    internal IEnumerable<WaitEntity> GetTreeItems()
    {
        yield return this;
        if (ChildWaits != null)
            foreach (var item in ChildWaits)
            {
                foreach (var item2 in item.GetTreeItems())
                {
                    yield return item2;
                }
            }
    }

    internal IEnumerable<WaitEntity> GetAllParent()
    {
        yield return this;
        if (ParentWait != null)
            ParentWait.GetAllParent();
    }

    internal virtual void OnAddWait()
    {
        if (!IsRoot) return;

        var waitGroups =
            GetTreeItems().
            Where(x => x.RuntimeClosureId != null).
            GroupBy(x => x.RuntimeClosureId);
        foreach (var group in waitGroups)
        {
            var mw = (MethodWaitEntity)
                group.FirstOrDefault(x => x is MethodWaitEntity mw && mw.ImmutableClosure != default);
            if (mw == default)
            {
                foreach (var wait in group)
                {
                    wait.RuntimeClosureId = null;
                }
                break;
            }

            var useOldWaitClosure =
                OldCompletedSibling != null &&
                OldCompletedSibling.RuntimeClosure != null &&
                OldCompletedSibling.CallerName == group.First().CallerName;

            var runtimeClosure = 
                useOldWaitClosure ?
                OldCompletedSibling.RuntimeClosure :
                new RuntimeClosure
                {
                    Id = mw.RuntimeClosureId.Value,
                    Value = mw.ImmutableClosure,
                    CallerName = mw.CallerName,
                };

            foreach (var wait in group)
            {
                //wait.RuntimeClosureId = null;
                wait.RuntimeClosure = runtimeClosure;
            }
        }
    }

    internal MethodWaitEntity GetChildMethodWait(string name)
    {
        if (this is TimeWaitEntity tw)
            return tw.TimeWaitMethod;

        var result = this
            .Flatten(x => x.ChildWaits)
            .FirstOrDefault(x => x.Name == name && x is MethodWaitEntity);
        if (result == null)
            throw new NullReferenceException($"No MethodWait with name [{name}] exist in ChildWaits tree [{Name}]");
        return (MethodWaitEntity)result;
    }

    public override string ToString()
    {
        return $"Name:{Name}, Type:{WaitType}, Id:{Id}, Status:{Status}";
    }

    public void OnSave()
    {
        var converter = new BinarySerializer();
        if (ExtraData != null)
            ExtraDataValue = converter.ConvertToBinary(ExtraData);
    }

    public void LoadUnmappedProps()
    {
        var converter = new BinarySerializer();
        if (ExtraDataValue != null)
            ExtraData = converter.ConvertToObject<WaitExtraData>(ExtraDataValue);
        if (FunctionState?.StateObject != null && CurrentFunction == null)
            CurrentFunction = (ResumableFunctionsContainer)FunctionState.StateObject;
    }

    /// <summary>
    /// Validate delegate that used for groupMatchFilter,AfterMatchAction,CancelAction and return:
    /// $"{method.DeclaringType.FullName}#{method.Name}"
    /// </summary>
    internal string ValidateMethod(Delegate callback, string methodName)
    {
        var method = callback.Method;
        var functionClassType = CurrentFunction.GetType();
        var declaringType = method.DeclaringType;
        var containerType = callback.Target?.GetType();

        var validConatinerCalss =
          (declaringType == functionClassType ||
          declaringType.Name == Constants.CompilerStaticLambdas ||
          declaringType.Name.StartsWith(Constants.CompilerClosurePrefix)) &&
          declaringType.FullName.StartsWith(functionClassType.FullName);

        if (validConatinerCalss is false)
            throw new Exception(
                $"For wait [{Name}] the [{methodName}:{method.Name}] must be a method in class " +
                $"[{functionClassType.Name}] or inline lambda method.");

        var hasOverload = functionClassType.GetMethods(Flags()).Count(x => x.Name == method.Name) > 1;
        if (hasOverload)
            throw new Exception(
                $"For wait [{Name}] the [{methodName}:{method.Name}] must not be over-loaded.");
        if (declaringType.Name.StartsWith(Constants.CompilerClosurePrefix))
            SetImmutableClosure(callback.Target);
        return $"{method.DeclaringType.FullName}#{method.Name}";
    }

    internal void SetImmutableClosure(object closure)
    {
        if (closure == default) return;

        var closureString =
               JsonConvert.SerializeObject(closure, ClosureContractResolver.Settings);
        //ClosureHash = closureString.GetHashCode();
        ImmutableClosure = JsonConvert.DeserializeObject(closureString, closure.GetType());
    }

    protected static BindingFlags Flags() =>
        BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;


    internal void SetLocals(object locals)
    {
        Locals = locals;
    }

    internal string LocalsDisplay()
    {
        var closure = RuntimeClosure?.Value;
        if (Locals == null && closure == null)
            return null;
        var result = new JObject();
        if (Locals != null && Locals.ToString() != "{}")
            result["Locals"] = Locals as JToken;
        if (closure != null && closure.ToString() != "{}")
            result["Closure"] = closure as JToken;
        if (result?.ToString() != "{}")
            return result.ToString()?.Replace("<", "").Replace(">", "");
        return null;
    }


    internal Wait ToWait() => new Wait(this);
}