using System;
using System.Linq;
using System.Reflection;
using AspectInjector.Broker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Publisher.Abstraction;
using ResumableFunctions.Publisher.InOuts;

namespace ResumableFunctions.Publisher.Helpers
{
    [Aspect(Scope.PerInstance)]
    public class PublishMethodAspect
    {
        private MethodCall _methodCall;
        private ILogger<PublishMethodAspect> _logger;
        private ICallPublisher _publishMethod;
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
            _publishMethod = ServiceProvider.GetService<ICallPublisher>();
            _methodCall = new MethodCall
            {
                MethodData = new MethodData
                {
                    MethodUrn = publishMethodAttribute.MethodIdentifier,
                    AssemblyName = "[External] " + Assembly.GetEntryAssembly()?.GetName().Name,
                    ClassName = metadata.DeclaringType.Name,
                    MethodName = metadata.Name,
                    //InputType = (metadata as MethodInfo).ReturnType.Name,
                    //OutputType = (metadata as MethodInfo).GetParameters()[0].ParameterType.Name
                },
                ServiceName = publishMethodAttribute.ToService
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
                _logger.LogError(ex, $"Error when try to push call for method [{name}]");
            }
        }
    }
}