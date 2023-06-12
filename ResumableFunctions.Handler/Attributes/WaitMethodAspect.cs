using AspectInjector.Broker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System.Diagnostics;
using System.Reflection;



namespace ResumableFunctions.Handler.Attributes
{


    [Aspect(Scope.Global, Factory = typeof(HangfireActivator))]
    public class WaitMethodAspect
    {
        private PushedCall _pushedCall;
        private readonly ICallPusher _callPusher;
        private readonly ILogger<WaitMethodAspect> _logger;
        public WaitMethodAspect(ICallPusher callPusher, ILogger<WaitMethodAspect> logger)
        {
            _callPusher = callPusher;
            _logger = logger;
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
            var pushResultAttribute = triggers.OfType<WaitMethodAttribute>().First();

            _pushedCall = new PushedCall
            {
                MethodData = new MethodData(metadata as MethodInfo)
                {
                    MethodUrn = pushResultAttribute.MethodUrn,
                    CanPublishFromExternal = pushResultAttribute.CanPublishFromExternal,
                },
            };
            if (args.Length > 0)
                _pushedCall.Data.Input = args[0];

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
                _pushedCall.Data.Output = result;
                _callPusher.PushCall(_pushedCall).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when try to pushe method call for method [{name}]");
            }
            //Console.WriteLine($"Method `{name}` executed and result is `{result}`");
        }
    }
}