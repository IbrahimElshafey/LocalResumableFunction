namespace ResumableFunctions.Handler.InOuts;

public class Closures
{
    public Dictionary<int, string> ClosuresDictionary { get; set; } = new();
    public string this[int functionId]
    {
        get => ClosuresDictionary.ContainsKey(functionId) ? ClosuresDictionary[functionId] : null;
        set
        {
            if (ClosuresDictionary.ContainsKey(functionId))
                ClosuresDictionary[functionId] = value;
            else
                ClosuresDictionary.Add(functionId, value);
        }
    }
}
