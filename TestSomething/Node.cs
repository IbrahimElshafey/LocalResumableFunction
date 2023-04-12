// See https://aka.ms/new-console-template for more information
public class Node
{
    public int Id { get; set; }
    public List<Node> Childs { get; set; }

    public IEnumerator<Node> CascadeGet()
    {
        yield return this;

        return child.CascadeGet();
    }
}