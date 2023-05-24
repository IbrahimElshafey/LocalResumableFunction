using AspectInjector.Broker;



namespace ResumableFunctions.Handler.Attributes
{
    [Aspect(Scope.PerInstance)]
    public class BackgroundTaskAspect
    {
        [Advice(Kind.Around)]
        public object ExecuteBackgroundTask(
            [Argument(Source.Name)] string name,
            [Argument(Source.Arguments)] object[] args,
            [Argument(Source.Type)] Type hostType,
            [Argument(Source.Target)] Func<object[], object> target,
            [Argument(Source.Triggers)] Attribute[] triggers) 
        {
            var result = target(args);
            return result;
        }
    }
}