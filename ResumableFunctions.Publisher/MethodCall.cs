namespace ResumableFunctions.Publisher
{
    public class MethodCall
    {
        public string MethodIdentifier { get; set; }
        public object Input { get; internal set; }
        public object Output { get; internal set; }
    }
}