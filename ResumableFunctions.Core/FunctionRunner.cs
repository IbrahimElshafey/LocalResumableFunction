using System.Reflection;
using ResumableFunctions.Core.InOuts;

namespace ResumableFunctions.Core;

internal class FunctionRunner : IAsyncEnumerator<Wait>
{
    private IAsyncEnumerator<Wait> _this;

    public FunctionRunner(Wait currentWait)
    {
        var functionRunnerType = currentWait.CurrentFunction.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(x => x.Name.StartsWith($"<{currentWait.RequestedByFunction.MethodName}>"));

        CreateRunner(functionRunnerType, currentWait.CurrentFunction);
        SetState(currentWait.StateAfterWait);
    }


    public FunctionRunner(ResumableFunction classInstance, MethodInfo resumableFunction, int? state = null)
    {
        var functionRunnerType = classInstance.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(x => x.Name.StartsWith($"<{resumableFunction.Name}>"));
        CreateRunner(functionRunnerType, classInstance);
        SetState(state ?? int.MinValue);
    }

    public bool ResumableFunctionExistInCode => _this != null;

    public Wait Current => _this.Current;

    public ValueTask DisposeAsync()
    {
        return _this.DisposeAsync();
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        var stateBeforeWait = GetState();
        var hasNext = await _this.MoveNextAsync();
        if (hasNext)
        {
            _this.Current.StateBeforeWait = stateBeforeWait;
            _this.Current.StateAfterWait = GetState();
        }

        return hasNext;
    }

    private void CreateRunner(Type functionRunnerType, ResumableFunction resumableFunctionLocal)
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

    private int GetState()
    {
        if (_this == null) return int.MinValue;
        var stateField = _this?.GetType().GetField("<>1__state");
        if (stateField != null) return (int)stateField.GetValue(_this);

        return int.MinValue;
    }

    private void SetState(int state)
    {
        if (_this != null)
        {
            var stateField = _this?.GetType().GetField("<>1__state");
            if (stateField != null) stateField.SetValue(_this, state);
        }
    }
}