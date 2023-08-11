namespace TestSomething;

internal class RecursiveFillStackTest
{
    internal void Run()
    {
        _ = CallFailedRequests();
    }
    public int Counter { get; set; }
    private async Task CallFailedRequests()
    {
        while (true)
        {
            Counter++;
            Console.WriteLine($"Counter: {Counter}");
        }
    }
}

