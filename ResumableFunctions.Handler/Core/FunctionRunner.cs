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

        CreateRunner(functionRunnerType, oldMatchedWait.Locals);
        SetRunnerFunctionClass(oldMatchedWait.CurrentFunction);
        SetState(oldMatchedWait.StateAfterWait);
        SetRunnerClosureField(oldMatchedWait.ClosureData?.Value);
        _oldMatchedWait = oldMatchedWait;
    }


    public FunctionRunner(
        ResumableFunctionsContainer classInstance,
        MethodInfo resumableFunction)
    {
        var functionRunnerType = classInstance.GetType()
            .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
            .FirstOrDefault(x => x.Name.StartsWith($"<{resumableFunction.Name}>"));
        CreateRunner(functionRunnerType);
        SetRunnerFunctionClass(classInstance);
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
            SetLocalsForNewWait();

            //set closure
            SetClosureForNewWait();
        }
        return hasNext;
    }

    private void SetClosureForNewWait()
    {
        var closureContinuation =
                        _oldMatchedWait != null &&
                        _oldMatchedWait.CallerName == CurrentWait.CallerName &&
                        _oldMatchedWait.ClosureDataId != null;
        var closureFields = GetClosureFields();
        if (closureContinuation)
        {
            //CurrentWait.ClosureDataId = _oldMatchedWait.ClosureDataId;
            CurrentWait.ClosureData = _oldMatchedWait.ClosureData;
            CurrentWait.OldCompletedSibling = _oldMatchedWait;
        }
        else if (closureFields.Any())
        {
            var activeClosure = closureFields.
                Select(x => x.GetValue(_functionRunner)).
                FirstOrDefault(x => x != null);
            if (activeClosure != null)
                CurrentWait.ClosureData = new PrivateData
                {
                    Value = activeClosure,
                    Type = PrivateDataType.Locals
                };
        }
    }

    private void SetLocalsForNewWait()
    {
        var localContinuation =
            _oldMatchedWait != null &&
            _oldMatchedWait.Locals != null;
        if (localContinuation)
        {
            _oldMatchedWait.Locals.Value = _functionRunner;
            //CurrentWait.LocalsId = _oldMatchedWait.LocalsId;
            CurrentWait.Locals = _oldMatchedWait.Locals;
        }
        else if (JsonConvert.SerializeObject(_functionRunner, ClosureContractResolver.Settings) != "{}")
        {
            CurrentWait.Locals = new PrivateData
            {
                Value = _functionRunner,
                Type = PrivateDataType.Locals
            };
        }
    }

    private bool RunnerHasValue()
    {
        var json = JsonConvert.SerializeObject(_functionRunner, ClosureContractResolver.Settings);
        return json != "{}";
    }

    private void CreateRunner(Type functionRunnerType, PrivateData oldLocals = null)
    {

        const string error = "Can't create a function runner.";
        if (functionRunnerType == null)
            throw new Exception(error);

        //use the old wait runner
        if (oldLocals?.Value.GetType() == functionRunnerType)
        {
            _functionRunner = (IAsyncEnumerator<Wait>)oldLocals.Value;
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

        if (oldLocals != null && oldLocals.Value is JObject jobject)
        {
            //Is same RF
            jobject.MergeIntoObject(_functionRunner);
        }
    }

    private void SetRunnerFunctionClass(ResumableFunctionsContainer functionClassInstance)
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
    private void SetRunnerClosureField(object closure)
    {
        var closureFields = GetClosureFields();
        closureFields.ForEach(closureField =>
        {
            if (closure is null && closureFields.Count == 1)
                closureField.SetValue(_functionRunner, Activator.CreateInstance(closureField.FieldType));
            else if (closure is JObject jobject)
            {
                var closureObject = jobject.ToObject(closureField.FieldType);
                closureField.SetValue(_functionRunner, closureObject ?? Activator.CreateInstance(closureField.FieldType));
            }
            else if (closure != null && closure.GetType() == closureField.FieldType)
                closureField.SetValue(_functionRunner, closure);
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