using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TestSomething
{
    internal class TestRewriteMatch : ResumableFunction
    {
        public int InstanceId { get; set; } = 5;

        [WaitMethod("TestMethodOne")]
        public int TestMethodOne(string input) => input.Length;

        [WaitMethod("TestMethodTwo")]
        public MethodOutput TestMethodTwo(MethodInput input) => new MethodOutput { TaskId = input.Id };

        public MethodWait WaitMethodOne()
        {
            var methodWait = new MethodWait<string, int>(TestMethodOne)
                        .MatchIf((x, y) => y == InstanceId && x == "12345" && y < Math.Max(10, 100))
                        .SetData((input, output) => InstanceId == output);
            methodWait.CurrentFunction = this;
            return methodWait;
        }

        public MethodWait WaitMethodTwo()
        {
            var methodWait = new MethodWait<MethodInput, MethodOutput>(TestMethodTwo)
                       .MatchIf((x, y) => y.TaskId == InstanceId)
                       .SetData((input, output) => InstanceId == output.TaskId);
            methodWait.CurrentFunction = this;
            return methodWait;
        }

        public void Test()
        {
            var wait = WaitMethodOne();
            var matchRewrite = new RewriteMatchExpression(wait);
            var method = (Func<string, int, TestRewriteMatch, bool>)matchRewrite.Result.Compile();
            var exprssionAsString = matchRewrite.Result.ToString();
            var result = method.Invoke("12345", 5, this);
            result = method.Invoke("123456", 6, this);


            var pushedCall = JsonConvert.DeserializeObject<JObject>("""
                {
                    "input":"123456",
                    "output":5
                }
                """);
            //var a = matchRewrite.WaitMatchValue;
            //foreach (var item in a.Children())
            //{
            //    if (item is JProperty property)
            //    {
            //        Console.WriteLine(pushedCall.SelectToken(property.Name));
            //        Console.WriteLine(property.Value);
            //        Console.WriteLine(JToken.DeepEquals(pushedCall.SelectToken(property.Name), property.Value));
            //    }
            //}
            //todo:query Jobject list
            //Match expression will be rewritten to use pushed call class
            //we will extract parts where == opertor used
            //we will ignore any other parts that use opertors like >,>=,....
        }
    }

    public class MethodInput
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class MethodOutput
    {
        public int TaskId { get; set; }
    }
}
