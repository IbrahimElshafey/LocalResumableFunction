using MessagePack;

namespace TestSomething;

[MessagePackObject]
public class Model2
{
    [Key(0)] public string Name { get; private set; }
    [Key(1)] public int[] Items { get; set; }
    [Key(2)] public byte BB { get; set; }
    [Key(3)] public DateTime DT { get; set; }
    [Key(5)] public dynamic Comp { get; set; }
    public void SetName(string name) => Name = name;
}