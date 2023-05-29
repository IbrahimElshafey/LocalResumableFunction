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
                        .MatchIf((x, y) => y == InstanceId || x == (InstanceId + 10).ToString() && y <= Math.Max(10, 100))
                        .SetData((input, output) => InstanceId == output);
            methodWait.CurrentFunction = this;
            return methodWait;
        }

        public MethodWait WaitMethodTwo()
        {
            var localVariable = "kjlklk";
            var methodWait = new MethodWait<MethodInput, MethodOutput>(TestMethodTwo)
                       .MatchIf((x, y) => y.TaskId == InstanceId + 10 || x.Id == InstanceId + 20 && localVariable == y.TaskId.ToString() && !x.IsMan)
                       .SetData((input, output) => InstanceId == output.TaskId);
            methodWait.CurrentFunction = this;
            return methodWait;
        }

        public void Test()
        {
            //var wait = WaitMethodTwo();
            //var matchRewrite = new RewriteMatchExpression(wait);
            //var method = (Func<MethodInput, MethodOutput, TestRewriteMatch, bool>)matchRewrite.Result.Compile();


            //var wait1 = WaitMethodOne();
            //var matchRewrite1 = new RewriteMatchExpression(wait1);
            //var method1 = (Func<string, int, TestRewriteMatch, bool>)matchRewrite1.Result.Compile();
            //var exprssionAsString1 = matchRewrite1.Result.ToString();
            //var result = method1.Invoke("12345", 5, this);
            //result = method1.Invoke("123456", 6, this);


            var pushedCall = JsonConvert.DeserializeObject<JObject>("""
                {
                    "input":"123456",
                    "output":6
                }
                """);

            //y == InstanceId || x == (InstanceId + 10).ToString() && y <= Math.Max(10, 100)
            Expression<Func<JObject, bool>> matchJson = pushedCall =>
                (int)pushedCall["output"] == InstanceId;
            var x = matchJson.Compile().Invoke(pushedCall);
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
        public bool IsMan { get; internal set; }
    }

    public class MethodOutput
    {
        public int TaskId { get; set; }
    }
}
