using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using MethodBoundaryAspect.Fody.Attributes;
using ResumableFunctions.Core;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ResumableFunctions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]

public sealed class WaitMethodImplementationAttribute : OnMethodBoundaryAspect, ITrackingIdetifier
{
    private PushedMethod _pushedMethod;
    private readonly ResumableFunctionHandler _functionHandler;
    private readonly ILogger<WaitMethodImplementationAttribute> _logger;

    public WaitMethodImplementationAttribute()
    {
        _functionHandler =
            CoreExtensions.GetServiceProvider()
            .GetService<ResumableFunctionHandler>();
        _logger =
            CoreExtensions.GetServiceProvider()
            .GetService<ILogger<WaitMethodImplementationAttribute>>();
    }
    public override object TypeId => nameof(WaitMethodImplementationAttribute);
    public string TrackingIdentifier { get; set; }

    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = false;
        _pushedMethod = new PushedMethod
        {
            MethodData = new MethodData(GetBaseMethod(args)) { TrackingId = TrackingIdentifier }
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
            if (args.Method.IsAsyncMethod())
            {
                dynamic output = args.ReturnValue;
                _pushedMethod.Output = output.Result;
            }

            _functionHandler.QueuePushedMethodProcessing(_pushedMethod).Wait();
            args.MethodExecutionTag = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when try to pushe method call for method [{args.Method.GetFullName()}]");
        }

    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)args.MethodExecutionTag)
            return;
        Console.WriteLine("On exception");
    }
}