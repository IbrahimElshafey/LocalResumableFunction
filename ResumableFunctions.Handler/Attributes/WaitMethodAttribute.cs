using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using MethodBoundaryAspect.Fody.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using ResumableFunctions.Handler.Core.Abstraction;

namespace ResumableFunctions.Handler.Attributes;

/// <summary>
///     Add this to the method you want to wait to.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]

public sealed class WaitMethodAttribute : OnMethodBoundaryAspect, ITrackingIdetifier, IDisposable
{
    internal static IServiceProvider ServiceProvider;
    private PushedCall _pushedCall;
    private readonly IServiceScope _scope;
    private readonly IPushedCallProcessor _pushedCallProcessor;
    private readonly ILogger<WaitMethodAttribute> _logger;

    public WaitMethodAttribute(string methodUrn, bool canPublishFromExternal = false)
    {
        MethodUrn = methodUrn;
        CanPublishFromExternal = canPublishFromExternal;
        if (ServiceProvider == null) return;
        _logger = ServiceProvider.GetService<ILogger<WaitMethodAttribute>>();
        try
        {
            _scope = ServiceProvider.CreateScope();
            _pushedCallProcessor = _scope.ServiceProvider.GetService<IPushedCallProcessor>();
        }
        catch (Exception ex)
        {
            _logger.LogError("Can't initiate PushedCallProcessor.", ex);
        }
    }

    /// <summary>
    /// used to enable developer to change method name an parameters and keep point to the old one
    /// </summary>
    public string MethodUrn { get; }
    public bool CanPublishFromExternal { get; }

    public const string AttributeId = nameof(WaitMethodAttribute);
    public override object TypeId => AttributeId;

    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = false;
        _pushedCall = new PushedCall
        {
            MethodData = new MethodData(args.Method as MethodInfo)
            {
                MethodUrn = MethodUrn,
                CanPublishFromExternal = CanPublishFromExternal,
            },
        };
        if (args.Arguments.Length > 0)
            _pushedCall.Input = args.Arguments[0];
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        try
        {
            _pushedCall.Output = args.ReturnValue;
            if (args.Method.IsAsyncMethod())
            {
                dynamic output = args.ReturnValue;
                _pushedCall.Output = output.Result;
            }


            _pushedCallProcessor.QueuePushedCallProcessing(_pushedCall).Wait();
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

    public void Dispose()
    {
        _scope.Dispose();
    }
}
