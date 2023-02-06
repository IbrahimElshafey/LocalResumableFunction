using System.Drawing;
using System.Transactions;
using System.Linq;
using System.Reflection;

public abstract class ResumableFunctionLocal
{
    public Wait<Input, Output> When<Input, Output>(Func<Input, Output> method)
    {
        var eventMethodAttributeExist = method.Method.GetCustomAttribute(typeof(EventMethodAttribute));
        if (eventMethodAttributeExist == null)
            throw new Exception($"You must add attribute [{nameof(EventMethodAttribute)}] to method {method.Method.Name}");
        var result = new Wait<Input, Output>
        {
            CallerInfo = method.Method,
            Instance = this,
        };
        return result;
    }


    internal static async Task WhenMethodCalled(PushedEvent pushedEvent)
    {

    }
}
