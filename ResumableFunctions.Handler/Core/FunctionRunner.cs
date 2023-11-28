using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Reflection;

namespace ResumableFunctions.Handler.Core;

public class FunctionRunner : IAsyncEnumerator<Wait>
{
    private IAsyncEnumerator<Wait> _functionRunner;
    private readonly WaitEntity _oldMatchedWait;

    public FunctionRunner(WaitEntity oldMatchedWait)
    {
        var functionRunnerType = oldMatchedWait.CurrentFunction.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(type =>
            type.Name.StartsWith($"<{oldMatchedWait.RequestedByFunction.MethodName}>") &&
            typeof(IAsyncEnumerable<Wait>).IsAssignableFrom(type));

        if (functionRunnerType == null)
            throw new Exception(
                $"Can't find resumable function [{oldMatchedWait?.RequestedByFunction?.MethodName}] " +
                $"in class [{oldMatchedWait?.CurrentFunction?.GetType().FullName}].");

        _oldMatchedWait = oldMatchedWait;
        ResumeLocals(oldMatchedWait.Locals, functionRunnerType);
        CreateRunnerIfNull(functionRunnerType);
        SetRunnerCallerRfCalss(oldMatchedWait.CurrentFunction);
        ResumeClosure(oldMatchedWait.ClosureData);
        SetState(oldMatchedWait.StateAfterWait);
    }

    public FunctionRunner(
        ResumableFunctionsContainer classInstance,
        MethodInfo resumableFunction)
    {
        var functionRunnerType = classInstance.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(x => x.Name.StartsWith($"<{resumableFunction.Name}>"));
        CreateRunnerIfNull(functionRunnerType);
        SetRunnerCallerRfCalss(classInstance);
        SetState(int.MinValue);
    }

    public FunctionRunner(IAsyncEnumerator<Wait> runner)
    {
        _functionRunner = runner;
    }

    public bool ResumableFunctionExistInCode => _functionRunner != null;

    public Wait Current => _functionRunner.Current;
    public WaitEntity CurrentWait => _functionRunner.Current.WaitEntity;

    public ValueTask DisposeAsync()
    {
        return _functionRunner.DisposeAsync();
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        var hasNext = await _functionRunner.MoveNextAsync();
        if (hasNext)
        {
            CurrentWait.StateAfterWait = GetState();
            //set locals for the new incoming wait
            SetLocalsForIncomingWait();

            //set closure
            SetClosureForIncomingWait();
        }
        return hasNext;
    }

    private void SetClosureForIncomingWait()
    {
        var closureContinuation =
                        _oldMatchedWait != null &&
                        _oldMatchedWait.CallerName == CurrentWait.CallerName &&
                        _oldMatchedWait.ClosureData != null;

        var closureFields = GetClosureFields();
        var activeClosure = closureFields?.
             Select(x => x.GetValue(_functionRunner)).
             LastOrDefault(x => x != null);//we may have two closures like <>c__DisplayClass0_0,<>c__DisplayClass0_1

        if (activeClosure == null) return;
        var callerName = CurrentWait.CallerName;

        if (closureContinuation)
        {
            _oldMatchedWait.ClosureData.Value = activeClosure;
            CurrentWait.ClosureData = _oldMatchedWait.ClosureData;
        }

        CurrentWait.ActionOnChildrenTree(wait =>
        {
            if (wait.CallerName == callerName)
                wait.SetClosureObject(activeClosure);
        });
    }

    private void SetLocalsForIncomingWait()
    {
        var localsContinuation =
            _oldMatchedWait != null &&
            _oldMatchedWait.Locals != null;
        if (localsContinuation)
        {
            _oldMatchedWait.Locals.Value = _functionRunner;
            CurrentWait.Locals = _oldMatchedWait.Locals;
        }
        else if (RunnerHasValue())
        {
            CurrentWait.Locals = new PrivateData
            {
                Value = _functionRunner,
                FunctionStateId = _oldMatchedWait?.FunctionStateId
            };
        }
    }

    private bool RunnerHasValue()
    {
        var json = JsonConvert.SerializeObject(_functionRunner, PrivateDataResolver.Settings);
        return json != "{}";
    }

    private void CreateRunnerIfNull(Type functionRunnerType)
    {
        if (_functionRunner != null) return;

        const string error = "Can't create a function runner.";
        if (functionRunnerType == null)
            throw new Exception(error);

        var ctor = functionRunnerType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance,
            new[] { typeof(int) });

        if (ctor == null)
            throw new Exception(error);

        _functionRunner = (IAsyncEnumerator<Wait>)ctor.Invoke(new object[] { -2 });


        if (_functionRunner == null)
            throw new Exception(error);
    }

    private void SetRunnerCallerRfCalss(ResumableFunctionsContainer functionClassInstance)
    {
        //set caller class for current function runner
        var thisField = _functionRunner
            .GetType()
            .GetFields()
            .FirstOrDefault(x => x.Name.EndsWith(Constants.CompilerCallerSuffix) && x.FieldType == functionClassInstance.GetType());
        thisField?.SetValue(_functionRunner, functionClassInstance);
    }

    private List<FieldInfo> GetClosureFields() => _functionRunner.
            GetType().
            GetFields(BindingFlags.Instance | BindingFlags.NonPublic).
            Where(x => x.FieldType.Name.StartsWith(Constants.CompilerClosurePrefix)).
            ToList();
    private void ResumeLocals(PrivateData oldLocals, Type functionRunnerType)
    {
        //use the old wait runner
        if (oldLocals?.Value.GetType() == functionRunnerType)
        {
            _functionRunner = (IAsyncEnumerator<Wait>)oldLocals.Value;
            return;
        }
        if (oldLocals != null && oldLocals.Value is JObject jobject)
        {
            _functionRunner = (IAsyncEnumerator<Wait>)jobject.ToObject(functionRunnerType);
        }
    }

    private void ResumeClosure(PrivateData closureData)
    {
        var closureFields = GetClosureFields();
        if (closureFields is null || closureFields.Count == 0) return;

        var closure = closureData?.Value;
        if (closure is null)
        {
            closureFields.ForEach(
                field => field.SetValue(_functionRunner, Activator.CreateInstance(field.FieldType)));
            return;
        }

        var activeField = closureFields.FirstOrDefault(x => x.FieldType.Name == closureData.TypeName);
        if (activeField is null) return;
        if (closure is JObject jobject)
        {
            var closureObject = jobject.ToObject(activeField.FieldType);
            activeField.SetValue(_functionRunner, closureObject ?? Activator.CreateInstance(activeField.FieldType));
        }
        else if (closure.GetType() == activeField.FieldType)
            activeField.SetValue(_functionRunner, closure);
    }

    private int GetState()
    {
        if (_functionRunner == null) return int.MinValue;
        var stateField = _functionRunner?.GetType().GetField("<>1__state");
        if (stateField != null) return (int)stateField.GetValue(_functionRunner);

        return int.MinValue;
    }

    private void SetState(int state)
    {
        if (_functionRunner != null)
        {
            var stateField = _functionRunner?.GetType().GetField("<>1__state");
            if (stateField != null) stateField.SetValue(_functionRunner, state);
        }
    }
}