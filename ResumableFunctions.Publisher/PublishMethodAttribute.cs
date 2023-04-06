using MethodBoundaryAspect.Fody.Attributes;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ResumableFunctions.Publisher;

/// <summary>
///     Add this to the method you want to 
///     push it's call to the a resumable function service.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]

public sealed class PublishMethodAttribute : OnMethodBoundaryAspect
{
    private MethodCall _methodCall;
    private ILogger<PublishMethodAttribute> _logger;
    private IPublishMethodCall _publishMethod;
    public PublishMethodAttribute(string methodIdetifier)
    {
        if (string.IsNullOrWhiteSpace(methodIdetifier))
            throw new ArgumentNullException("MethodIdentifier can't be null or empty.");
        MethodIdentifier = methodIdetifier;
        _logger = CoreExtensions.GetServiceProvider().GetService<ILogger<PublishMethodAttribute>>();
        _publishMethod = CoreExtensions.GetServiceProvider().GetService<IPublishMethodCall>();
    }

    /// <summary>
    /// used to enable developer to change method name an parameters and keep point to the old one
    /// </summary>
    public string MethodIdentifier { get; }
    public override object TypeId => nameof(PublishMethodAttribute);

    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = false;
        _methodCall = new MethodCall
        {
            MethodIdentifier = MethodIdentifier
        };
        if (args.Arguments.Length > 0)
            _methodCall.Input = args.Arguments[0];
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        try
        {
            _methodCall.Output = args.ReturnValue;
            if (args.Method.IsAsyncMethod())
            {
                dynamic output = args.ReturnValue;
                _methodCall.Output = output.Result;
            }

            //call `/api/ResumableFunctions/MethodMethod`
            //_functionHandler.QueuePushedMethodProcessing(_pushedMethod).Wait();
            _publishMethod.Publish(_methodCall);
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
