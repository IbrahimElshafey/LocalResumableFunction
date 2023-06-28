namespace TestSomething;

internal class TimerTest
{
    private int _time = 1000;
    private int _round = 1;
    internal async Task Run()
    {
        Action<string> func = (input) => Console.WriteLine("Method " + input);
        Console.WriteLine($"Wait {_time / 1000} seconds.");
        await Task.Delay(_time);
        _round++;
        //_time = _round * 1000; 1,2,3,4
        //_time = (int)Math.Pow(2, _round - 1) * 1000;// 1,2,4,8,16
        await Run();
    }
}
