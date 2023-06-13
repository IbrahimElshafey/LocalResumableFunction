using FastExpressionCompiler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TestSomething
{
    public class TestRewriteMatch : ResumableFunction
    {
        public int InstanceId { get; set; } = 5;

        [PushCall("TestMethodOne")]
        public int TestMethodOne(string input) => input.Length;

        [PushCall("TestMethodTwo")]
        public MethodOutput TestMethodTwo(MethodInput input) => new MethodOutput { TaskId = input.Id };

        public MethodWait WaitMethodOne()
        {
            var methodWait = new MethodWait<string, int>(TestMethodOne)
                        .MatchIf((x, y) => y == InstanceId || x == (InstanceId + 10).ToString() && y <= Math.Max(10, 100))
                        .SetData((input, output) => InstanceId == output);
            methodWait.CurrentFunction = this;
            return methodWait;
        }

        private int[] IntArrayMethod() => new int[] { 12, 13, 14, 15, };
        public MethodWait WaitMethodTwo()
        {
            var localVariable = "kjlklk";
            var methodWait = new MethodWait<MethodInput, MethodOutput>(TestMethodTwo)
                       .MatchIf((x, y) =>
                       !(y.TaskId == InstanceId + 10 &&
                       x.Id > 12) &&
                       //x.Id == InstanceId + 20 &&
                       //y.DateProp == DateTime.Today &&
                       //y.ByteArray == new byte[] { 12, 13, 14, 15, } ||
                       //y.IntArray[0] == IntArrayMethod()[2] ||
                       //y.IntArray == IntArrayMethod() &&
                       //11 + 1 == 12 &&
                       //y.GuidProp == new Guid("ab62534b-2229-4f42-8f4e-c287c82ec760") &&
                       //y.EnumProp == (StackBehaviour.Pop1 | StackBehaviour.Pop1_pop1) ||
                       y.EnumProp == StackBehaviour.Popi_popi_popi &&
                       x.IsMan
                       )
                       .SetData((input, output) => InstanceId == output.TaskId);
            methodWait.CurrentFunction = this;
            return methodWait;
        }

        public void Run()
        {
            //var aggregateMethod = typeof(Enumerable).GetMethod("Aggregate");
            var pushedCall = JsonConvert.DeserializeObject<JObject>("""
                {
                    "input":"12345",
                    "output":{"X":254,"Z":255}
                }
                """);
          

            TestWithComplexTypes();
            //TestWithBasicTypes();

            //Expression<Func<JObject, bool>> matchJson = pushedCall =>
            //    (int)pushedCall["output"] == InstanceId;
            //var x = matchJson.Compile().Invoke(pushedCall);

            Expression point = () => (PointXY)pushedCall.SelectToken("output").ToObject(typeof(PointXY));
            Expression<Func<JObject, string>> GetIds =
                (jobject) => string.Join("#", new[]
                {
                    pushedCall.SelectToken("input").ToString(),
                    pushedCall.SelectToken("output.X").ToString()
                });
            var id = GetIds.CompileFast()(pushedCall);

        }
        class PointXY
        {
            public int X { get; set; }
            public int Y { get; set; }
        }
        private void TestWithComplexTypes()
        {
            var wait = WaitMethodTwo();
            var matchRewrite1 = new MatchExpressionWriter(wait.MatchExpression, this);

            var pushedCall = JsonConvert.DeserializeObject<JObject>("""
                {
                    "input":{"Id":12,"Name":"Hello","IsMan":true},
                    "output":{
                        "TaskId":20,
                        "GuidProp":"7ec03d6d-64e3-4240-bbdc-a143e327a3fc",
                        "DateProp":"",
                        "ByteArray":"",
                        "IntArray":"",
                        "EnumProp":7,
                    }
                }
                """);
            var getId = matchRewrite1.CallMandatoryPartExpressionDynamic;
            var compile = getId.Compile();
            var value = compile.Invoke(pushedCall);
            //var matchRewrite = new RewriteMatchExpression(wait);
            //var method = (Func<MethodInput, MethodOutput, TestRewriteMatch, bool>)matchRewrite.MatchExpression.CompileFast();
        }

        private void TestWithBasicTypes()
        {
            var wait1 = WaitMethodOne();
            var matchRewrite1 = new MatchExpressionWriter(wait1.MatchExpression, this);
            var method1 = (Func<string, int, TestRewriteMatch, bool>)matchRewrite1.MatchExpressionWithConstants.CompileFast();
            var exprssionAsString1 = matchRewrite1.MatchExpressionWithConstants.ToString();
            var result = method1.Invoke("12345", 5, this);
            result = method1.Invoke("123456", 6, this);


            var pushedCall1 = JsonConvert.DeserializeObject<JObject>("""
                {
                    "input":"12345",
                    "output":5
                }
                """);
            var pushedCall2 = JsonConvert.DeserializeObject<JObject>("""
                {
                    "input":"123456",
                    "output":6
                }
                """);
            //var jsonCompiled = (Func<JObject, bool>)matchRewrite1.MatchExpressionWithJson.CompileFast();
            //result = jsonCompiled.Invoke(pushedCall1);
            //result = jsonCompiled.Invoke(pushedCall2);
        }
    }
}
