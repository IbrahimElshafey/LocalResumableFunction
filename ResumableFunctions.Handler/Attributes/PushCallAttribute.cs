using AspectInjector.Broker;

namespace ResumableFunctions.Handler.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    [Injection(typeof(PushCallAspect), Inherited = true)]
    public class PushCallAttribute : Attribute, ITrackingIdentifier
    {
        public PushCallAttribute(string methodUrn, bool canPublishFromExternal = false)
        {
            MethodUrn = methodUrn;
            CanPublishFromExternal = canPublishFromExternal;
        }

        public string MethodUrn { get; }
        public bool CanPublishFromExternal { get; }

        public const string AttributeId = nameof(PushCallAttribute) + "1f220128-d0f7-4dac-ad81-ff942d68942c";
        public override object TypeId => AttributeId;

        public override string ToString()
        {
            return $"{MethodUrn},{CanPublishFromExternal}";
        }
    }
}