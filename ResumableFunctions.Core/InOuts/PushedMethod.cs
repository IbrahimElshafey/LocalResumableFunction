using Newtonsoft.Json.Linq;
using ResumableFunctions.Core.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace ResumableFunctions.Core.InOuts;
public class PushedMethod
{
    public int Id { get; internal set; }
    public MethodData MethodData { get; internal set; }
    public object Input { get; internal set; }
    public object Output { get; internal set; }

    public int MatchedWaitsCount { get; internal set; }
    public int CompletedWaitsCount { get; internal set; }

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

    internal void ConvertJObject(MethodInfo methodInfo)
    {
        try
        {
            var inputType = methodInfo.GetParameters()[0].ParameterType;
            if (Input is JObject inputJson)
            {
                Input = inputJson.ToObject(inputType);
            }
            else
                Input = Convert.ChangeType(Input.ToString(), inputType);
        }
        catch (Exception ex)
        {

        }

        try
        {
            if (Output is JObject outputJson)
            {
                if (methodInfo.IsAsyncMethod())
                    Output = outputJson.ToObject(methodInfo.ReturnType.GetGenericArguments()[0]);
                else
                    Output = outputJson.ToObject(methodInfo.ReturnType);
            }
            else
                Output = Convert.ChangeType(Output.ToString(), methodInfo.ReturnType);
        }
        catch (Exception)
        {
        }

    }
}