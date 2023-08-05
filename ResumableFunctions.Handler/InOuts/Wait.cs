using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler.InOuts;

public abstract class Wait : IEntityWithUpdate, IEntityWithDelete, IOnSaveEntity
{
    public int Id { get; internal set; }
    public DateTime Created { get; internal set; }
    public string Name { get; internal set; }

    public WaitStatus Status { get; internal set; } = WaitStatus.Waiting;
    public bool IsFirst { get; internal set; }
    public bool WasFirst { get; internal set; }
    public int StateBeforeWait { get; internal set; }
    public int StateAfterWait { get; internal set; }
    public bool IsRootNode { get; internal set; }
    public bool IsReplay { get; internal set; }

    [NotMapped]
    public WaitExtraData ExtraData { get; internal set; }
    public byte[] ExtraDataValue { get; internal set; }

    public int? ServiceId { get; set; }

    public WaitType WaitType { get; internal set; }
    public DateTime Modified { get; internal set; }
    public string ConcurrencyToken { get; internal set; }
    public bool IsDeleted { get; internal set; }

    internal ResumableFunctionState FunctionState { get; set; }

    internal int FunctionStateId { get; set; }


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
    public object Closure { get; private set; }

    internal int? ParentWaitId { get; set; }
    public string Path { get; internal set; }

    [NotMapped]
    internal ResumableFunctionsContainer CurrentFunction { get; set; }

    internal bool CanBeParent => this is FunctionWait || this is WaitsGroup;
    internal int? CallId { get; set; }
    public int InCodeLine { get; internal set; }
    public string CallerName { get; internal set; }

    protected object CallMethodByName(string methodFullName, params object[] parameters)
    {
        var parts = methodFullName.Split('#');
        var methodName = parts[1];
        var className = parts[0];
        object instance = CurrentFunction;
        var classType = instance.GetType();
        var methodInfo = classType.GetMethod(methodName, Flags());

        if (methodInfo != null)
            return methodInfo.Invoke(instance, parameters);

        var lambdasClass = classType.Assembly.GetType(className);
        if (lambdasClass != null)
        {
            methodInfo = lambdasClass.GetMethod(methodName, Flags());
            //instance = JsonConvert.DeserializeObject(Closure ?? "{}", lambdasClass);
            instance = GetClosureAsType(lambdasClass);

            //set parent class who call
            var thisField = lambdasClass.GetFields().FirstOrDefault(x => x.Name.EndsWith("__this"));
            thisField?.SetValue(instance, CurrentFunction);
            if (methodInfo != null)
            {
                var result = methodInfo.Invoke(instance, parameters);
                SetClosure(instance, true);
                return result;
            }
        }

        throw new NullReferenceException(
            $"Can't find method [{methodName}] in class [{classType.Name}]");
    }



    internal async Task<Wait> GetNextWait()
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
                var nextWait = functionRunner.Current;
                var replaySuffix = nextWait is ReplayRequest ? " - Replay" : "";

                FunctionState.AddLog(
                    $"Get next wait [{functionRunner.Current.Name}{replaySuffix}] " +
                    $"after [{Name}]", LogType.Info, StatusCodes.WaitProcessing);

                nextWait.ParentWaitId = ParentWaitId;
                FunctionState.StateObject = CurrentFunction;
                nextWait.FunctionState = FunctionState;
                nextWait.RequestedByFunctionId = RequestedByFunctionId;

                return nextWait = functionRunner.Current;
            }

            return null;
        }
        catch (Exception ex)
        {
            FunctionState.AddError(
                $"An error occurred after resuming execution after wait [{this}].", StatusCodes.WaitProcessing, ex);
            FunctionState.Status = FunctionStatus.InError;
            throw;
        }
        finally
        {
            CurrentFunction.Logs.ForEach(log => log.EntityType = nameof(ResumableFunctionState));
            FunctionState.Logs.AddRange(CurrentFunction.Logs);
            FunctionState.Status =
              CurrentFunction.HasErrors() || FunctionState.HasErrors() ?
              FunctionStatus.InError :
              FunctionStatus.InProgress;
        }
    }

    public virtual bool IsCompleted() => Status == WaitStatus.Completed;


    public virtual void CopyCommonIds(Wait oldWait)
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
                result = new MethodWait
                {
                    TemplateId = methodWait.TemplateId,
                    MethodGroupToWaitId = methodWait.MethodGroupToWaitId,
                    MethodToWaitId = methodWait.MethodToWaitId
                };
                break;
            case FunctionWait:
                result = new FunctionWait();
                break;
            case WaitsGroup waitsGroup:
                result = new WaitsGroup
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

    private void CopyCommon(Wait fromWait)
    {
        Name = fromWait.Name;
        Status = fromWait.Status;
        IsFirst = fromWait.IsFirst;
        StateBeforeWait = fromWait.StateBeforeWait;
        StateAfterWait = fromWait.StateAfterWait;
        Closure = fromWait.Closure;
        IsRootNode = fromWait.IsRootNode;
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

    internal virtual bool IsValidWaitRequest()
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
            FunctionState.Status = FunctionStatus.InError;
        }
        return hasErrors is false;
    }


    internal void ActionOnParentTree(Action<Wait> action)
    {
        action(this);
        if (ParentWait != null)
            ParentWait.ActionOnParentTree(action);
    }

    internal void ActionOnChildrenTree(Action<Wait> action)
    {
        action(this);
        if (ChildWaits != null)
            foreach (var item in ChildWaits)
                item.ActionOnChildrenTree(action);
    }

    internal MethodWait GetChildMethodWait(string name)
    {
        if (this is TimeWait tw)
            return tw.TimeWaitMethod;

        var result = this
            .Flatten(x => x.ChildWaits)
            .FirstOrDefault(x => x.Name == name && x is MethodWait);
        if (result == null)
            throw new NullReferenceException($"No MethodWait with name [{name}] exist in ChildWaits tree [{Name}]");
        return (MethodWait)result;
    }

    public override string ToString()
    {
        return $"Name:{Name}, Type:{WaitType}, Id:{Id}, Status:{Status}";
    }

    public void OnSave()
    {
        var converter = new BinaryToObjectConverter();
        if (ExtraData != null)
            ExtraDataValue = converter.ConvertToBinary(ExtraData);
    }

    public void LoadUnmappedProps()
    {
        var converter = new BinaryToObjectConverter();
        if (ExtraDataValue != null)
            ExtraData = converter.ConvertToObject<WaitExtraData>(ExtraDataValue);
        if (FunctionState?.StateObject != null && CurrentFunction == null)
            CurrentFunction = (ResumableFunctionsContainer)FunctionState.StateObject;
    }

    protected string ValidateMethod(Delegate del, string methodName)
    {
        //if we serialized the instance below we will be able to use local variables

        var method = del.Method;
        var functionClassType = CurrentFunction.GetType();
        var declaringType = method.DeclaringType;
        var containerType = del.Target?.GetType();


        //if (declaringType.Name.StartsWith("<>c__DisplayClass") && runnerType == null)
        //    throw new Exception(
        //        $"You use local variables in method [{CallerName}] for callback [{methodName}] " +
        //        $"while you wait [{Name}], " +
        //        $"using local variables is allowed only inside the resumable functions body.");

        var validConatinerCalss =
          (declaringType == functionClassType ||
          declaringType.Name == "<>c" ||
          declaringType.Name.StartsWith("<>c__DisplayClass")) &&
          declaringType.FullName.StartsWith(functionClassType.FullName);

        if (validConatinerCalss is false)
            throw new Exception(
                $"For wait [{Name}] the [{methodName}:{method.Name}] must be a method in class " +
                $"[{functionClassType.Name}] or inline lambda method.");

        var hasOverload = functionClassType.GetMethods(Flags()).Count(x => x.Name == method.Name) > 1;
        if (hasOverload)
            throw new Exception(
                $"For wait [{Name}] the [{methodName}:{method.Name}] must not be over-loaded.");
        if (declaringType.Name.StartsWith("<>c__DisplayClass"))
            SetClosure(del.Target, true);

        var runnerType = functionClassType
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(type =>
            type.Name.StartsWith($"<{CallerName}>") &&
            typeof(IAsyncEnumerable<Wait>).IsAssignableFrom(type));
        if (declaringType.Name.StartsWith("<>c__DisplayClass") && runnerType != null)
        {
            var closureField =
                    runnerType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .FirstOrDefault(x => x.FieldType == method.DeclaringType);
            if (closureField is null)
                throw new Exception(
                    $"You use local variables in method [{CallerName}] for callback [{methodName}] " +
                    $"while you wait [{Name}], " +
                    $"The compiler didn't create the closure as a field, " +
                    $"to force it to create a one use/list your local varaibles at the end of the resuamble function. somthing like:\n" +
                    $"Console.WriteLine(<your_local_var>);");
        }
        return $"{method.DeclaringType.FullName}#{method.Name}";
    }


    protected static BindingFlags Flags() =>
        BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    protected object GetClosureAsType(Type lambdasClass)
    {
        Closure = Closure is JObject jobject ? jobject.ToObject(lambdasClass) : Closure;
        return Closure;
    }

    internal void SetClosure(object closure, bool deepCopy = false)
    {
        if (deepCopy && closure != null)
        {
            var closureString =
                JsonConvert.SerializeObject(
                    closure,
                    new JsonSerializerSettings { ContractResolver = IgnoreThisField.Instance });
            Closure = JsonConvert.DeserializeObject(closureString, closure.GetType());
        }
        else
            Closure = closure;
    }
}