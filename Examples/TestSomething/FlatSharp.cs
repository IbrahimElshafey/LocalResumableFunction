using FlatSharp;

public class MyObject
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class FlatSharpTest
{
    public static void Run()
    {
        // Create an object to serialize.
        MyObject myObject = new MyObject
        {
            Name = "John Doe",
            Age = 30
        };
        //// Create a serializer.
        //FlatBufferSerializer serializer = new FlatBufferSerializer();

        //// Serialize the object to a string.
        //var serializedObject = serializer.Compile<MyObject>();
        //serializedObject.wr

        //// Print the serialized object.
        //Console.WriteLine(serializedObject);
    }
}