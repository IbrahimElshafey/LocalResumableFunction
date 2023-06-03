using MemoryPack;
using System;
using static BinaryPackTest;

public class MemoryPackTest
{
    public void Run()
    {
        // Create an object to serialize.
        var myObject = new MyObject<int>
        {
            Name = "John Doe",
            Age = 30
        };
        var bin = MemoryPackSerializer.Serialize(myObject);
        var val = MemoryPackSerializer.Deserialize<MyObject<int>>(bin);
        var newObj = new MyObject<int>();
        var val2= MemoryPackSerializer.Deserialize(bin,ref newObj);
    }
}