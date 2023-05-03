
using System.Text.Json;

namespace ResumableFunctions.Publisher
{
    public class MethodCall
    {
        public string MethodUrn { get; set; }
        public string ServiceName { get; set; }
        public object Input { get; internal set; }
        public object Output { get; internal set; }
        public override string ToString()
        {
            return $"[MethodUrn:{MethodUrn}, \n" +
                $"Input:{JsonSerializer.Serialize(Input)}, \n" +
                $"Output:{JsonSerializer.Serialize(Output)} ]";
        }
    }
}