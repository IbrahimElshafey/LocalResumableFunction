using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using MethodBoundaryAspect.Fody.Attributes;
using ResumableFunctions.Core;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Core.Abstraction;

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
        _pushedMethod.Output = args.ReturnValue;
        //var isTaskResult = args.ReturnValue.GetType().GetGenericTypeDefinition() == typeof(Task<>);
        if (CoreExtensions.IsAsyncMethod(args.Method))
        {
            dynamic output = args.ReturnValue;
            _pushedMethod.Output = output.Result;
        }
        //todo: main method must wait until this completes ==> Ue hangfire
        //_ = new ResumableFunctionHandler().MethodCalled(_pushedMethod);
        CoreExtensions.GetServiceProvider().GetService<IProcessPushedMethodCall>().MethodCalled(_pushedMethod);
        args.MethodExecutionTag = true;
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)args.MethodExecutionTag)
            return;
        Console.WriteLine("On exception");
    }
}