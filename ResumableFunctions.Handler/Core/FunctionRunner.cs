using System.Reflection;
using Hangfire.Common;
using Newtonsoft.Json;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Core;

internal class FunctionRunner : IAsyncEnumerator<Wait>
{
    private IAsyncEnumerator<Wait> _functionRunner;

    public FunctionRunner(Wait currentWait)
    {
        var functionRunnerType = currentWait.CurrentFunction.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(type =>
            type.Name.StartsWith($"<{currentWait.RequestedByFunction.MethodName}>") &&
            typeof(IAsyncEnumerable<Wait>).IsAssignableFrom(type));

        if (functionRunnerType == null)
            throw new Exception(
                $"Can't find resumable function [{currentWait?.RequestedByFunction?.MethodName}] " +
                $"in class [{currentWait?.CurrentFunction?.GetType().FullName}].");

        CreateRunner(functionRunnerType);
        SetFunctionCallerInstance(currentWait.CurrentFunction);
        SetClosure(currentWait.Closure);//for colsure continuation
        SetState(currentWait.StateAfterWait);
    }


    public FunctionRunner(ResumableFunctionsContainer classInstance, MethodInfo resumableFunction, int? state = null)
    {
        var functionRunnerType = classInstance.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(x => x.Name.StartsWith($"<{resumableFunction.Name}>"));
        CreateRunner(functionRunnerType);
        SetFunctionCallerInstance(classInstance);
        //SetClosure(null);
        SetState(state ?? int.MinValue);
    }

    public bool ResumableFunctionExistInCode => _functionRunner != null;

    public Wait Current => _functionRunner.Current;

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
            _functionRunner.Current.StateBeforeWait = stateBeforeWait;
            _functionRunner.Current.StateAfterWait = GetState();
            //set closure after move to next
            if (_functionRunner.Current?.Closure == null)
                _functionRunner.Current.SetClosure(GetClosure());
        }
        return hasNext;
    }


    private void CreateRunner(Type functionRunnerType)
    {
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

    private void SetFunctionCallerInstance(ResumableFunctionsContainer resumableFunctionLocal)
    {
        //set parent class who call
        var thisField = _functionRunner.GetType().GetFields().FirstOrDefault(x => x.Name.EndsWith("__this"));
        thisField?.SetValue(_functionRunner, resumableFunctionLocal);
    }

    private void SetClosure(object closure)
    {
        _functionRunner.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
        .Where(x => x.FieldType.Name.StartsWith("<>c__DisplayClass"))
        .ToList()
        .ForEach(closureField =>
        {
            try
            {
                closureField.SetValue(_functionRunner, closure);
            }
            catch 
            {}
        });
    }

    private object GetClosure()
    {
        if (_functionRunner == null) return null;

        var closureField =
           _functionRunner?.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
           .FirstOrDefault(closureField => closureField.FieldType.Name.StartsWith("<>c__DisplayClass") && closureField.GetValue(_functionRunner) != null);
        if (closureField != null)
        {
            return closureField.GetValue(_functionRunner);
        }
        return null;
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