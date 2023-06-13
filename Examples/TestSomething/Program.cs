namespace TestSomething;

internal class Program
{
    private static async Task Main(string[] args)
    {
        switch (10)
        {
            case 1:
                TestTreeCascadeAction(); break;
            case 2:
                await TestAspectInjectorAsync(); break;
            case 3:
                new TestRewriteMatch().Run(); break;
            case 4:
                new MessagePackTest().Test(); break;
            case 5:
                new MemoryPackTest().Run(); break;
            case 6:
                new CerasTest().Run(); break;
            case 7:
                new NuqleonSerializeExpression().Run();
                break;
            case 8:
                new BinaryPackTest().Run();
                break;
            case 9:
                new SetDepsTest().Run();
                break;
            case 10:
                new GetMethodInfoTest().Run();
                break;
        }
    }

    private static async Task TestAspectInjectorAsync()
    {
        try
        {
            //new Node().MethodWithPushAspectApplied("Hello");
            await new Node().MethodWithPushAspectAppliedAsync("Hello from async");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            Console.ReadLine();
        }
       
    }

    private static void TestTreeCascadeAction()
    {
        Console.WriteLine("Hello, World!");
        var node = new Node
        {
            Id = 1,
            Childs = new List<Node>
            {
                new Node {
                    Id=2,
                    Childs=new List<Node> {
                        new Node {
                            Id=3, Childs=new List<Node>
                            {
                                new Node { Id=4,}
                            }
                        }
                    }},
                new Node { Id=5,}
            }
        };

        node.CascadeAction(x =>
        {
            x.Id2 = x.Id * 10;
            Console.WriteLine($"Node:{x.Id},{x.Id2}");
        });
        foreach (var item in node.CascadeFunc(x => x))
        {
            Console.WriteLine($"Node Id:{item.Id}");
        }
    }
}