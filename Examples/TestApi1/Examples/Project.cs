namespace TestApi1.Examples;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsResubmit { get; set; } = false;

    public override string ToString()
    {
        return $"[Name:{Name}, Description:{Description}, IsResubmit:{IsResubmit}]";
    }
}