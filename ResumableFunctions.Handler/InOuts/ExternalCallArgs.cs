using Newtonsoft.Json.Linq;

namespace ResumableFunctions.Handler.InOuts;

public class ExternalCallArgs
{
    public string MethodIdentifier { get; set; }
    public dynamic Input { get; set; }
    public dynamic Output { get; set; }
}