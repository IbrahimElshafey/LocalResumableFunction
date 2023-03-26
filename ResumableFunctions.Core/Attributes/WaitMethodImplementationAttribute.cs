using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using MethodBoundaryAspect.Fody.Attributes;
using ResumableFunctions.Core;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ResumableFunctions.Core.Attributes;

public sealed class WaitMethodImplementationAttribute : OnMethodBoundaryAspect
{
    private PushedMethod _pushedMethod;
    public override object TypeId => nameof(WaitMethodImplementationAttribute);

    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = false;
        _pushedMethod = new PushedMethod
        {
            MethodData = new MethodData(GetBaseMethod(args))
        };
        if (args.Arguments.Length > 0)
            _pushedMethod.Input = args.Arguments[0];
    }

    private MethodBase GetBaseMethod(MethodExecutionArgs args)
    {
        var type = args.Method.DeclaringType;
        foreach (Type interf in type.GetInterfaces())
        {
            foreach (MethodInfo method in interf.GetMethods())
            {
                bool sameSiganture = 
                    method.Name == args.Method.Name && 
                    method.GetParameters()[0]?.ParameterType == args.Arguments[0]?.GetType();
                if (sameSiganture)
                    return method;
            }
        }
        return args.Method;
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
            //todo:log error
        }
        
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)args.MethodExecutionTag)
            return;
        Console.WriteLine("On exception");
    }
}