namespace ResumableFunctions.Data.Abstraction.Entities
{
    public class WaitMethodIdentifier : MethodIdentifier
    {
        public MethodsGroup MethodGroup { get; internal set; }
        public int MethodGroupId { get; internal set; }
        //public List<MethodWaitEntity> Waits { get; internal set; }
    }

}
