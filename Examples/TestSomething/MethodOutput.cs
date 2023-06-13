using System.Reflection.Emit;

namespace TestSomething;

public class MethodOutput
{
    public int TaskId { get; set; }
    public Guid GuidProp { get; set; }
    public DateTime DateProp { get; set; }

    public byte[] ByteArray { get; set; }
    public int[] IntArray { get; set; }
    public StackBehaviour EnumProp { get; set; }
}