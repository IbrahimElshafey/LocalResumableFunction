namespace LocalResumableFunction;

internal class Program
{
    private static async Task Main(string[] assemblyNames)
    {
        await ResumableFunctionHandler.RegisterFirstWaits(assemblyNames);
        var example = new Example();
        example.ProjectSubmitted(new Project
        {
            Id = 1,
            Name = "Project One",
            Description = "Description"
        });
    }
}