using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.BaseUse;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts.Entities;
using System.Reflection;

namespace ResumableFunctions.Handler.Core;

public class FunctionRunner : IAsyncEnumerator<Wait>
{
    private IAsyncEnumerator<Wait> _functionRunner;

    public FunctionRunner(WaitEntity oldCompletedWait)
    {
        var functionRunnerType = oldCompletedWait.CurrentFunction.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(type =>
            type.Name.StartsWith($"<{oldCompletedWait.RequestedByFunction.MethodName}>") &&
            typeof(IAsyncEnumerable<Wait>).IsAssignableFrom(type));

        if (functionRunnerType == null)
            throw new Exception(
                $"Can't find resumable function [{oldCompletedWait?.RequestedByFunction?.MethodName}] " +
                $"in class [{oldCompletedWait?.CurrentFunction?.GetType().FullName}].");

        CreateRunner(functionRunnerType, oldCompletedWait.Locals);
        SetFunctionCallerInstance(oldCompletedWait.CurrentFunction);
        SetState(oldCompletedWait.StateAfterWait);
        SetClosure(oldCompletedWait.Closure);
    }


    public FunctionRunner(ResumableFunctionsContainer classInstance, MethodInfo resumableFunction, int? state = null, object closure = null)
    {
        var functionRunnerType = classInstance.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(x => x.Name.StartsWith($"<{resumableFunction.Name}>"));
        CreateRunner(functionRunnerType);
        SetFunctionCallerInstance(classInstance);
        SetState(state ?? int.MinValue);
        if (closure != null)
            SetClosure(closure);
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
        var stateBeforeWait = GetState();
        var hasNext = await _functionRunner.MoveNextAsync();
        if (hasNext)
        {
            CurrentWait.StateBeforeWait = stateBeforeWait;
            CurrentWait.StateAfterWait = GetState();
            //set locals for the new incoming wait
            if (CurrentWait.Locals == null)
                CurrentWait.SetLocals(_functionRunner);
        }
        return hasNext;
    }


    private void CreateRunner(Type functionRunnerType, object oldLocals = null)
    {

        const string error = "Can't create a function runner.";
        if (functionRunnerType == null)
            throw new Exception(error);

        if (oldLocals?.GetType() == functionRunnerType)
        {
            _functionRunner = (IAsyncEnumerator<Wait>)oldLocals;
            return;
        }


        var ctor = functionRunnerType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance,
            new[] { typeof(int) });

        if (ctor == null)
            throw new Exception(error);

        _functionRunner = (IAsyncEnumerator<Wait>)ctor.Invoke(new object[] { -2 });


        if (_functionRunner == null)
            throw new Exception(error);

        if (oldLocals != null && oldLocals is JObject jobject)
        {
            jobject.MergeIntoObject(_functionRunner);
            //JsonConvert.PopulateObject(jobject.CreateReader(), _functionRunner, LocalsContractResolver.Settings);
        }
    }

    private void SetFunctionCallerInstance(ResumableFunctionsContainer functionClassInstance)
    {
        //set caller class for current function runner
        var thisField = _functionRunner
            .GetType()
            .GetFields()
            .FirstOrDefault(x => x.Name.EndsWith(Constants.CompilerCallerSuffix) && x.FieldType == functionClassInstance.GetType());
        thisField?.SetValue(_functionRunner, functionClassInstance);
    }

    private void SetClosure(object closure)
    {
        if (closure == null)
            return;
        _functionRunner.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(x => x.FieldType.Name.StartsWith(Constants.CompilerClosurePrefix))
            .ToList()
            .ForEach(closureField =>
            {
                try
                {
                    if (closure is JObject jobject)
                    {
                        var closureObject = jobject.ToObject(closureField.FieldType);
                        closureField.SetValue(_functionRunner, closureObject ?? Activator.CreateInstance(closureField.FieldType));
                    }
                    else if (closure.GetType() == closureField.FieldType)
                        closureField.SetValue(_functionRunner, closure);
                }
                catch
                { }
            });
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