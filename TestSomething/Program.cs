// See https://aka.ms/new-console-template for more information
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
foreach (var item in node.CascadeGet())
{
    Console.WriteLine($"Node Id:{item.Id}");
}