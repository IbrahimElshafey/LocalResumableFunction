// See https://aka.ms/new-console-template for more information
using Aspects.PushResult;

public class Node
{
    public int Id { get; set; }
    public int Id2 { get; set; }
    public List<Node> Childs { get; set; }

    List<Node> listFlatten = new List<Node>();

    public void CascadeAction(Action<Node> action)
    {
        action(this);
        if (Childs != null)
            foreach (var item in Childs)
                item.CascadeAction(action);
    }
    public IEnumerable<T> CascadeFunc<T>(Func<Node, T> func)
    {
        yield return func(this);
        if (Childs != null)
            foreach (var item in Childs)
                item.CascadeFunc(func);
    }

    [PushResult("URN:bghjhjkolk", true)]
    internal int MethodWithPushAspectApplied(string input)
    {
        return Random.Shared.Next() - input.Length;
    }

    [PushResult("URN:bghjhjkolk-Async", true)]
    internal async Task<int> MethodWithPushAspectAppliedAsync(string input)
    {
        await Task.Delay(1000);
        return Random.Shared.Next() - input.Length;
    }
}