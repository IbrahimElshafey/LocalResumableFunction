using System.Text.Json;

namespace ResumableFunctions.Publisher.InOuts
{
    public class MethodCall
    {
       
        public MethodData MethodData { get; set; }
        public string[] ToServices { get; set; }
        public object Input { get; set; }
        public object Output { get; set; }
        public override string ToString()
        {
            return $"[MethodUrn:{MethodData?.MethodUrn}, \n" +
                $"Input:{JsonSerializer.Serialize(Input)}, \n" +
                $"Output:{JsonSerializer.Serialize(Output)} ]";
        }
    }


}