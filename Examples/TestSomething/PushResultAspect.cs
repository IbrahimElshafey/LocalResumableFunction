using AspectInjector.Broker;
using System.Diagnostics;
using System.Reflection;



namespace Aspects.PushResult
{
    [Aspect(Scope.Global)]
    public class PushResultAspect
    {
        [Advice(Kind.Before)]
        public void OnEntry(
            [Argument(Source.Name)] string name,
            [Argument(Source.Arguments)] object[] args,
            [Argument(Source.Instance)] object instance,
            [Argument(Source.ReturnType)] Type retType,
            [Argument(Source.Metadata)] MethodBase metadata,
            [Argument(Source.Triggers)] Attribute[] triggers
            )
        {
            var pushResultAttribute = triggers.OfType<PushToResumableFunctionEngineAttribute>().First();
           
            Console.WriteLine($"Before executing method `{name}` with input `{args.Aggregate((x,y)=>$"{x},{y}")}` and attribute `{pushResultAttribute}`");
            Console.WriteLine($"Instance is: `{instance}`");
            Console.WriteLine($"Return type is: `{retType.FullName}`");
            Console.WriteLine($"Metadata is: `{metadata.Name}` of type `{metadata.GetType().Name}`");
        }

        [Advice(Kind.After)]
        public void OnExit(
           [Argument(Source.Name)] string name,
           [Argument(Source.ReturnValue)] object result
           )
        {
            Console.WriteLine($"Method `{name}` executed and result is `{result}`");
        }

        
    }
}