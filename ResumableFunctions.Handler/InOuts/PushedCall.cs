using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ResumableFunctions.Handler.InOuts;
public class PushedCall : IEntityWithDelete
{
    public int Id { get; internal set; }
    public MethodData MethodData { get; internal set; }
    public object Input { get; internal set; }
    public object Output { get; internal set; }

    public int MatchedWaitsCount { get; internal set; }
    public int CompletedWaitsCount { get; internal set; }


    public bool IsFinished => MatchedWaitsCount == CompletedWaitsCount;
    public string RefineMatchModifier
    {
        get
        {
            var name = nameof(MethodWait.RefineMatchModifier);
            if (Input is string s && s.StartsWith(name))
                return s.Substring(name.Length);
            if (Output is string output && output.StartsWith(name))
                return output.Substring(name.Length);
            if (Input is JObject inputJson && inputJson[name] != null)
                return inputJson[name].ToString();
            if (Output is JObject outputJson && outputJson[name] != null)
                return outputJson[name].ToString();
            return null;
        }
    }

    public DateTime Created { get; internal set; }

    public bool IsDeleted { get; internal set; }
}
