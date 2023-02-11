using LocalResumableFunction.InOuts;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;
namespace LocalResumableFunction.Helpers
{
    public class RewriteSetDataExpression
    {
        public LambdaExpression Result { get; private set; }
        public RewriteSetDataExpression(MethodWait wait)
        {
            var contextProp = wait.SetDataExpression;
            if (contextProp.Body is not MemberExpression me)
                throw new Exception("When you call `MethodWait.SetProp` the body must be `MemberExpression`");


            var functionDataParam = Parameter(wait.CurrntFunction.GetType(), "functionData");
            var dataPramterAccess = me.GetDataParamterAccess(functionDataParam);
            if (dataPramterAccess.IsFunctionData && dataPramterAccess.NewExpression != null)
            {
                var MethodDataParam = Parameter(wait.GetType(), "MethodData");
                var isGenericList = dataPramterAccess.NewExpression.Type.IsGenericType &&
                    dataPramterAccess.NewExpression.Type.GetGenericTypeDefinition() == typeof(List<>);
                if (isGenericList)
                {
                    var body = Call(dataPramterAccess.NewExpression, dataPramterAccess.NewExpression.Type.GetMethod("Add"), MethodDataParam);
                    Result = Lambda(body, functionDataParam, MethodDataParam);
                }
                else
                {
                    Expression body = Assign(dataPramterAccess.NewExpression, MethodDataParam);
                    var block = Block(new[] { body, Empty() });
                    Result = Lambda(block, functionDataParam, MethodDataParam);
                }

            }
            else
                throw new Exception("When you call `MethodWait.SetProp` the body must be `MemberExpression` that use the `Data` property.");
        }
    }

}
