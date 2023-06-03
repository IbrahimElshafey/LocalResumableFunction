
using BinaryPack;
using System.Dynamic;
using System.Reflection;

internal class BinaryPackTest
{
    internal void Run()
    {
        MyObject model = new MyObject
        {
            Name = "John Doe",
            Age = 30
        };
        var data = BinaryConverter.Serialize(model);

        // Deserialize the model
        var loaded = BinaryConverter.Deserialize<MyObject>(data);
        var loaded2 = BinaryConverter.Deserialize<MyObject2>(data);

        MethodInfo method = typeof(BinaryConverter).GetMethod("Deserialize", 1, new[] { typeof(byte[]) });
        MethodInfo generic = method.MakeGenericMethod(typeof(MyObject3));
        var loaded3 = generic.Invoke(null, new[] { data });
    }

    public class MyObject2
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }
    public class MyObject3
    {
        public int Age { get; set; }
        public string Name { get; set; }
        public bool? IsHappy { get; set; }
    }

}