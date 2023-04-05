using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using MethodBoundaryAspect.Fody.Attributes;
using ResumableFunctions.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ResumableFunctions.Core.Attributes;

/// <summary>
///     Add this to the method you want to wait to.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]

public sealed class WaitMethodAttribute : OnMethodBoundaryAspect, ITrackingIdetifier
{
    private PushedMethod _pushedMethod;
    private readonly ResumableFunctionHandler _functionHandler;
    private readonly ILogger<WaitMethodAttribute> _logger;

    public WaitMethodAttribute()
    {
        var serviceProvider = CoreExtensions.GetServiceProvider();
        if (serviceProvider == null) return;
        _functionHandler = serviceProvider.GetService<ResumableFunctionHandler>();
        _logger = CoreExtensions.GetServiceProvider().GetService<ILogger<WaitMethodAttribute>>();
    }

    /// <summary>
    /// used to enable developer to change method name an parameters and keep point to the old one
    /// </summary>
    public string TrackingIdentifier { get; set; }
    public override object TypeId => nameof(WaitMethodAttribute);

    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = false;
        _pushedMethod = new PushedMethod
        {
            MethodData = new MethodData(args.Method) { TrackingId = TrackingIdentifier },
        };
        if (args.Arguments.Length > 0)
            _pushedMethod.Input = args.Arguments[0];
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
