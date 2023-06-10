using Nuqleon.Json.Expressions;
using ResumableFunctions.Handler.InOuts;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Helpers;

public class WaitExpressionsHash : System.Linq.Expressions.ExpressionVisitor
{
    public byte[] Hash { get; internal set; }

    public WaitExpressionsHash(MethodWait methodWait)
    {
        var serializer = new ExpressionSerializer();
        var sb = new StringBuilder();
        if (methodWait.MatchExpression != null)
        {
            ChangeInputAndOutputNames(methodWait.MatchExpression);
            sb.Append(serializer.Serialize(methodWait.MatchExpression.ToExpressionSlim()));
        }
        if (methodWait.SetDataExpression != null)
        {
            ChangeInputAndOutputNames(methodWait.SetDataExpression);
            sb.Append(serializer.Serialize(methodWait.SetDataExpression.ToExpressionSlim()));
        }
        var data = Encoding.Unicode.GetBytes(sb.ToString());
        Hash = MD5.HashData(data);
    }

    private void ChangeInputAndOutputNames(LambdaExpression expression)
    {
        var changeParametersVisitor = new GenericVisitor();
        var inputArg = Parameter(expression.Parameters[0].Type, "input");
        var outputArg = Parameter(expression.Parameters[1].Type, "output");
        changeParametersVisitor.OnVisitParamter(ChangeParameterName);
        System.Linq.Expressions.Expression ChangeParameterName(ParameterExpression node)
        {
            //rename output
            var isOutput = node == expression.Parameters[1];
            if (isOutput) return outputArg;

            //rename input
            var isInput = node == expression.Parameters[0];
            if (isInput) return inputArg;

            return base.VisitParameter(node);
        }
    }

}
