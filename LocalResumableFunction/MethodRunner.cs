using LocalResumableFunction.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LocalResumableFunction
{
    internal class MethodRunner : IAsyncEnumerator<Wait>
    {
        private IAsyncEnumerator<Wait> _this;

        public MethodRunner(Wait currentWait)
        {
            var functionRunnerType = currentWait.CurrntFunction.GetType()
                .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SuppressChangeType)
                .FirstOrDefault(x => x.Name.StartsWith($"<{currentWait.RequestedByFunction.MethodName}>"));

            if (functionRunnerType == null) { _this = null; return; }
            ConstructorInfo? ctor = functionRunnerType.GetConstructor(
               BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance,
               new Type[] { typeof(int) });

            if (ctor == null) { _this = null; return; }
            _this = (IAsyncEnumerator<Wait>)ctor.Invoke(new object[] { -2 });

            if (_this == null) { _this = null; return; }
            //set parent class who call
            var thisField = functionRunnerType.GetFields().FirstOrDefault(x => x.Name.EndsWith("__this"));
            //var thisField = FunctionRunnerType.GetField("<>4__this");
            thisField?.SetValue(_this, this);
            //var xx=thisField?.GetValue(_activeRunner);

            //set in start state
            SetState(currentWait.StateAfterWait);
        }

        public Wait Current => throw new NotImplementedException();

        public ValueTask DisposeAsync()
        {
           return _this.DisposeAsync();
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return _this.MoveNextAsync();
        }

        internal int GetState()
        {
            if (_this == null) return int.MinValue;
            var stateField = _this?.GetType().GetField("<>1__state");
            if (stateField != null)
            {
                return (int)stateField.GetValue(_this);
            }
            return int.MinValue;
        }
        internal void SetState(int state)
        {
            if (_this != null)
            {
                var stateField = _this?.GetType().GetField("<>1__state");
                if (stateField != null)
                {

                    stateField.SetValue(_this, state);
                }
            }
        }
    }
}
