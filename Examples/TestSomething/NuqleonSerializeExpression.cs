using System.Linq.Expressions;
using System.Linq.Expressions.Bonsai.Serialization;
using ResumableFunctions.Handler.InOuts;
using FastExpressionCompiler;

namespace TestSomething
{
    internal class NuqleonSerializeExpression
    {
        public void Run()
        {
            //UseSerialize_Linq();
            //UseNuqleon();
            TestDesrialization();
        }

        private void TestDesrialization()
        {
            var match = @"{""Context"":{""Members"":[[""P"",1,""FormId"",[],3]],""Types"":[[""::"",""ClientOnboarding.InOuts.RegistrationForm"",0],[""::"",""ClientOnboarding.InOuts.RegistrationResult"",0],[""::"",""ClientOnboarding.Workflow.ClientOnboardingWorkflow"",0],[""::"",""System.Int32"",1],[""::"",""System.Func`4"",1],[""::"",""System.Boolean"",1],[""<>"",4,[0,1,2,5]]],""Assemblies"":[""ClientOnboarding, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",""System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e""],""Version"":""0.9.0.0""},""Expression"":[""=>"",6,["">"",[""."",0,[""$"",0,1]],["":"",0,3]],[[0,""input""],[1,""output""],[2,""functionInstance""]]]}";
            var setData = @"{""Context"":{""Types"":[[""::"",""ClientOnboarding.InOuts.RegistrationForm"",0],[""::"",""ClientOnboarding.InOuts.RegistrationResult"",0],[""::"",""ClientOnboarding.Workflow.ClientOnboardingWorkflow"",0],[""::"",""System.Void"",1],[""::"",""System.Action`3"",1],[""<>"",4,[0,1,2]]],""Assemblies"":[""ClientOnboarding, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",""System.Private.CoreLib, Version=7.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e""],""Version"":""0.9.0.0""},""Expression"":[""=>"",5,[""{...}"",3,[],[[""default"",3]]],[[0,""input""],[1,""output""],[2,""functionInstance""]]]}";
            var obj = new ObjectSerializer();
            var serializer = new ExpressionSlimBonsaiSerializer(
                obj.GetJsonSerializer,
                obj.GetJsonDeserializer,
                new Version("0.9"));
            var testSerializer = new ExpressionSerializer();
            var matchExp = testSerializer.Deserialize(match).ToExpression();
            var setDataExp = testSerializer.Deserialize(setData).ToExpression();
        }

        private void UseNuqleon()
        {
            Expression<Func<InputComplex, WaitExtraData, string>> exp = (x, y) => (x.Id + y.JobId.Length * 10 - 30).ToString();
            var obj = new ObjectSerializer();
            var serializer = new ExpressionSlimBonsaiSerializer(
                obj.GetJsonSerializer,
                obj.GetJsonDeserializer,
                new Version("0.9"));
            var expSlim = exp.ToExpressionSlim();
            var testSerializer = new ExpressionSerializer();
            var serailzed = testSerializer.Serialize(expSlim);
            var back = testSerializer.Deserialize(serailzed);

            var code = back.ToCSharpString();
            var exp2 = (LambdaExpression)back.ToExpression();



            var inputComplex = new InputComplex { Id = 1, Name = "kjkil" };
            var extraData = new WaitExtraData { JobId = "jkkjmk" };
            var exp1Compiled = exp.CompileFast();
            var exp2Compiled = (Func<InputComplex, WaitExtraData, string>)exp2.CompileFast();
            var v1 = exp1Compiled(inputComplex, extraData);
            var v2 = exp2Compiled(inputComplex, extraData);
        }


    }
}
