using System.Reflection;
using BinaryPack;

namespace TestSomething;

internal class BinaryPackTest
{
    internal void Run()
    {
        var model = new MyObject<int>
        {
            Name = "John Doe",
            Age = 30,
            SubComplex = new() { X = 100, Y = 200 },
            TProp = 5000
        };
        model.SetPrivate();
        var data = BinaryConverter.Serialize(model);

        // Deserialize the model
        var loaded = BinaryConverter.Deserialize<MyObject<int>>(data);
        var loaded2 = BinaryConverter.Deserialize<MyObject2>(data);

        var method = typeof(BinaryConverter).GetMethod("Deserialize", 1, new[] { typeof(byte[]) });
        var generic = method.MakeGenericMethod(typeof(MyObject3));
        //var loaded2 = generic.Invoke(null, new[] { data });

      
        var loaded3 = generic.Invoke(null, new[] { data });
    }
    public class MyObject<T>
    {
        public void SetPrivate() => PrivateString = "111111111";
        private string PrivateString { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public SubComplex SubComplex { get; set; }
        public T TProp { get; set; }
    }
    public class MyObject2
    {
        private string PrivateString { get; set; }
        public int Age { get; set; }
        public string Name { get; set; }
        public SubComplexClone SubComplex { get; set; }

    }
    public class MyObject3
    {
        //public int Age { get; set; }
        public string Name { get; set; }
        //public bool? IsHappy { get; set; }
    }

    public class SubComplex
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
    public class SubComplexClone
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}