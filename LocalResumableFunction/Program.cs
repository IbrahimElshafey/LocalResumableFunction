using LocalResumableFunction;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var example = new Example();
        var runner = example.Start().GetAsyncEnumerator();
        await runner.MoveNextAsync();
        await  ResumableFunctionLocal.WaitRequested(runner.Current);
        example.ProjectSubmitted(new Project
        {
            Id = 1,
            Name = "Project One",
            Description = "Description"
        });
    }


}