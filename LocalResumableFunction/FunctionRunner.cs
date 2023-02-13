using System.Reflection;
using LocalResumableFunction.InOuts;

namespace LocalResumableFunction;

internal class FunctionRunner : IAsyncEnumerator<Wait>
{
    private IAsyncEnumerator<Wait> _this;

    public FunctionRunner(Wait currentWait)
    {
        var functionRunnerType = currentWait.CurrntFunction.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(x => x.Name.StartsWith($"<{currentWait.RequestedByFunction.MethodName}>"));

        CreateRunner(functionRunnerType,currentWait.CurrntFunction);
        SetState(currentWait.StateAfterWait);
    }


    public FunctionRunner(ResumableFunctionLocal classInstance, MethodInfo resumableFunction)
    {
        var functionRunnerType = classInstance.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(x => x.Name.StartsWith($"<{resumableFunction.Name}>"));
        CreateRunner(functionRunnerType,classInstance);
        SetState(int.MinValue);
    }

    public Wait Current => _this.Current;

    public ValueTask DisposeAsync()
    {
        return _this.DisposeAsync();
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return _this.MoveNextAsync();
    }

    private void CreateRunner(Type? functionRunnerType, ResumableFunctionLocal resumableFunctionLocal)
    {
        if (functionRunnerType == null)
        {
            _this = null;
            return;
        }

        var ctor = functionRunnerType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance,
            new[] { typeof(int) });

        if (ctor == null)
        {
            _this = null;
            return;
        }

        _this = (IAsyncEnumerator<Wait>)ctor.Invoke(new object[] { -2 });

        if (_this == null)
        {
            _this = null;
            return;
        }

        //set parent class who call
        var thisField = functionRunnerType.GetFields().FirstOrDefault(x => x.Name.EndsWith("__this"));
        //var thisField = FunctionRunnerType.GetField("<>4__this");
        thisField?.SetValue(_this, resumableFunctionLocal);
        //var xx=thisField?.GetValue(_activeRunner);

        //set in start state
    }

    internal int GetState()
    {
        if (_this == null) return int.MinValue;
        var stateField = _this?.GetType().GetField("<>1__state");
        if (stateField != null) return (int)stateField.GetValue(_this);

        return int.MinValue;
    }

    internal void SetState(int state)
    {
        if (_this != null)
        {
            var stateField = _this?.GetType().GetField("<>1__state");
            if (stateField != null) stateField.SetValue(_this, state);
        }
    }
}