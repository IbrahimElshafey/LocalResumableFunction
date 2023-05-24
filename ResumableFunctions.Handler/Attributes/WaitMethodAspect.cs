using AspectInjector.Broker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResumableFunctions.Handler.Core.Abstraction;
using ResumableFunctions.Handler.InOuts;
using System.Diagnostics;
using System.Reflection;



namespace ResumableFunctions.Handler.Attributes
{

    [Aspect(Scope.Global)]
    public class WaitMethodAspect : IDisposable
    {
        internal static IServiceProvider ServiceProvider;
        private PushedCall _pushedCall;
        private IServiceScope _scope;
        private IPushedCallProcessor _pushedCallProcessor;
        private ILogger<WaitMethodAspect> _logger;

        

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
            InitDepends();
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
                _pushedCall.Input = args[0];

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
                _pushedCall.Output = result;
                _pushedCallProcessor.QueuePushedCallProcessing(_pushedCall).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when try to pushe method call for method [{name}]");
            }
            //Console.WriteLine($"Method `{name}` executed and result is `{result}`");
        }

        public void Dispose()
        {
            _scope.Dispose();
        }

        private void InitDepends()
        {
            _logger = ServiceProvider.GetService<ILogger<WaitMethodAspect>>();
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
    }
}