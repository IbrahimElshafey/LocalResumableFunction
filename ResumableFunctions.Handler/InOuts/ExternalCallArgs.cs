using Newtonsoft.Json.Linq;

namespace ResumableFunctions.Handler.InOuts;

public class ExternalCallArgs
{
    public string MethodUrn { get; set; }
    public dynamic Input { get; set; }
    public dynamic Output { get; set; }
}