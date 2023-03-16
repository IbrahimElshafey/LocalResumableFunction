using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using MethodBoundaryAspect.Fody.Attributes;
using System.Reflection;

namespace LocalResumableFunction.Attributes;

public sealed class WaitMethodImplementationAttribute : OnMethodBoundaryAspect
{
    private PushedMethod _pushedMethod;
    public override object TypeId => nameof(WaitMethodAttribute);

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
        if (Extensions.IsAsyncMethod(args.Method))
        {
            dynamic output = args.ReturnValue;
            _pushedMethod.Output = output.Result;
        }
        //todo: main method must wait until this completes ==> Ue hangfire
        //_ = new ResumableFunctionHandler().MethodCalled(_pushedMethod);
        new ResumableFunctionHandler().MethodCalled(_pushedMethod).Wait();
        args.MethodExecutionTag = true;
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)args.MethodExecutionTag)
            return;
        Console.WriteLine("On exception");
    }
}