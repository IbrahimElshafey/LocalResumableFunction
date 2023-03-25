using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using MethodBoundaryAspect.Fody.Attributes;
using ResumableFunctions.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ResumableFunctions.Core.Attributes;

/// <summary>
///     Add this to the method you want to wait to.
/// </summary>
public sealed class WaitMethodAttribute : OnMethodBoundaryAspect
{
    private PushedMethod _pushedMethod;
    public override object TypeId => nameof(WaitMethodAttribute);

    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = false;
        _pushedMethod = new PushedMethod
        {
            MethodData = new MethodData(args.Method)
        };
        if (args.Arguments.Length > 0)
            _pushedMethod.Input = args.Arguments[0];
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        try
        {
            _pushedMethod.Output = args.ReturnValue;
            //var isTaskResult = args.ReturnValue.GetType().GetGenericTypeDefinition() == typeof(Task<>);
            if (args.Method.IsAsyncMethod())
            {
                dynamic output = args.ReturnValue;
                _pushedMethod.Output = output.Result;
            }

            CoreExtensions.GetServiceProvider()
                .CreateScope().ServiceProvider
                .GetService<ResumableFunctionHandler>()
                .QueuePushedMethodProcessing(_pushedMethod).Wait();
            args.MethodExecutionTag = true;
        }
        catch (Exception)
        {
        }
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)args.MethodExecutionTag)
            return;
        Console.WriteLine("On exception");
    }
}
