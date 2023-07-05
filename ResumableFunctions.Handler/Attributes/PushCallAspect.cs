using System.Reflection;
using AspectInjector.Broker;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.Attributes
{


    [Aspect(Scope.PerInstance, Factory = typeof(HangfireActivator))]
    public class PushCallAspect
    {
        private PushedCall _pushedCall;
        private readonly ICallPusher _callPusher;
        private readonly ILogger<PushCallAspect> _logger;
        public PushCallAspect(ICallPusher callPusher, ILogger<PushCallAspect> logger)
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
            var pushResultAttribute = triggers.OfType<PushCallAttribute>().First();

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
           [Argument(Source.ReturnValue)] object result//,
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
                _logger.LogError(ex, $"Error when try to push call for method [{name}]");
            }
            //Console.WriteLine($"Method `{name}` executed and result is `{result}`");
        }
    }
}