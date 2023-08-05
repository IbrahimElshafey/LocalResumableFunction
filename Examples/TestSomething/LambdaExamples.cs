namespace TestSomething;

internal class LambdaExamples
{
    private int _age = 30;
    public int Id { get; set; } = 100;
    public string Name { get; set; } = "Ibrahim";
    internal void Run()
    {

    }
    public void CaptureNothing0()
    {
        Action<int, string> captureNothingFunc0 = (x, y) => Console.WriteLine($"{x}{y}");
    }
    public void CaptureNothing()
    {
        Func<int, string, object> captureNothingFunc = (x, y) => $"{x}{y}";
    }
    public void CaptureInstancePublicProp()
    {
        Func<int, string, object> captureInstancePublicProp = (x, y) => $"{x + Id}{y + Name}";
    }
    public void CaptureLocalVariable()
    {
        var localVar = 100;
        Func<int, string, object> captureInstancePublicProp = (x, y) => $"{x + localVar}{y}";
    }
    public void CaptureLocalVariableAndInstanceProp()
    {
        var localVar = 100;
        Func<int, string, object> captureInstancePublicProp = (x, y) => $"{x + localVar}{y + Name}";
    }
    public void CaptureInstancePrivateProp()
    {
        Func<int, string, object> captureInstancePublicProp = (x, y) => $"{x + _age}{y}";
    }
}

