using AspectInjector.Broker;

namespace ResumableFunctions.Publisher;

/// <summary>
///     Add this to the method you want to 
///     push it's call to the a resumable function service.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[Injection(typeof(PublishMethodAspect), Inherited = true)]
public sealed class PublishMethodAttribute : Attribute
{
    public PublishMethodAttribute(string methodIdetifier, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(methodIdetifier))
            throw new ArgumentNullException("MethodIdentifier can't be null or empty.");
        MethodIdentifier = methodIdetifier;
        ServiceName = serviceName;
    }

    /// <summary>
    /// used to enable developer to change method name an parameters and keep point to the old one
    /// </summary>
    public string MethodIdentifier { get; }
    public string ServiceName { get; }
    public override object TypeId => nameof(PublishMethodAttribute);
}
