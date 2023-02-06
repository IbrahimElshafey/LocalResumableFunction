using LocalResumableFunction;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var example = new Example();
        var runner = example.Start().GetAsyncEnumerator();
        await runner.MoveNextAsync();
        var wait = runner.Current;
        example.ProjectSubmitted(new Project
        {
            Id = 1,
            Name = "Project One",
            Description = "Description"
        });
        example.AskManagerToApprove(10);
    }


}