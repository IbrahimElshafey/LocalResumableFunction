namespace TestSomething;
using System.Reflection;

internal class SerializeActionCall
{
    internal void Run()
    {
        GetFunc<int, int>(PlusTen);
        GetFunc<int, int>(x => x + 10);
        
    }

    private void GetFunc<X, Y>(Func<X, Y> function)
    {
        var methodInfo = function.Method;
        var className = methodInfo.DeclaringType.FullName;
        var assembly = methodInfo.DeclaringType.Assembly.FullName;
        var inputType = typeof(X);
        var outputType = typeof(Y);
        var method =
            Assembly.Load(assembly)
            .GetType(className)
            .GetMethod(methodInfo.Name, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Console.WriteLine(method.Invoke(Activator.CreateInstance(methodInfo.DeclaringType), new object[] { 10 }));
    }

    private int PlusTen(int i) => i + 10;
}

