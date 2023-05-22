using AspectInjector.Broker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace ResumableFunctions.Publisher;

[Aspect(Scope.Global)]
public class PublishMethodAspect
{
    private MethodCall _methodCall;
    private ILogger<PublishMethodAspect> _logger;
    private IPublishCall _publishMethod;
    internal static IServiceProvider ServiceProvider;

    public PublishMethodAspect()
    {
        if (ServiceProvider == null) return;
        _logger = ServiceProvider.GetService<ILogger<PublishMethodAspect>>();
    }

    [Advice(Kind.Before)]
    public void OnEntry(
        //[Argument(Source.Name)] string name,
        [Argument(Source.Arguments)] object[] args,
        //[Argument(Source.Instance)] object instance,
        //[Argument(Source.ReturnType)] Type retType,
        [Argument(Source.Metadata)] MethodBase metadata,
        [Argument(Source.Triggers)] Attribute[] triggers
        )
    {
        var publishMethodAttribute = triggers.OfType<PublishMethodAttribute>().First();

        _logger = ServiceProvider.GetService<ILogger<PublishMethodAspect>>();
        _publishMethod = ServiceProvider.GetService<IPublishCall>();
        _methodCall = new MethodCall
        {
            MethodUrn = publishMethodAttribute.MethodIdentifier,
            ServiceName = publishMethodAttribute.ServiceName
        };
        if (args.Length > 0)
            _methodCall.Input = args[0];

        //Console.WriteLine($"Before executing method `{name}` with input `{args.Aggregate((x,y)=>$"{x},{y}")}` and attribute `{pushResultAttribute}`");
        //Console.WriteLine($"Instance is: `{instance}`");
        //Console.WriteLine($"Return type is: `{retType.FullName}`");
        //Console.WriteLine($"Metadata is: `{metadata.Name}` of type `{metadata.GetType().Name}`");
    }

    [Advice(Kind.After)]
    public void OnExit(
       [Argument(Source.Name)] string name,
       [Argument(Source.ReturnValue)] object result
       //[Argument(Source.Metadata)] MethodBase metadata
       )
    {
        try
        {
            _methodCall.Output = result;
            _publishMethod.Publish(_methodCall);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when try to pushe method call for method [{name}]");
        }
    }
}
