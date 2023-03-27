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