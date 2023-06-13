using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Helpers;

public class WaitExpressionsHash : System.Linq.Expressions.ExpressionVisitor
{
    public byte[] Hash { get; internal set; }
    public LambdaExpression MatchExpression { get; internal set; }
    public LambdaExpression SetDataExpression { get; internal set; }

    public WaitExpressionsHash(LambdaExpression matchExpression, LambdaExpression setDataExpression)
    {
        var serializer = new ExpressionSerializer();
        var sb = new StringBuilder();
        if (matchExpression != null)
        {
            MatchExpression =
                (LambdaExpression)ChangeInputAndOutputNames(matchExpression);
            sb.Append(serializer.Serialize(matchExpression.ToExpressionSlim()));
        }
        if (setDataExpression != null)
        {
            SetDataExpression =
               (LambdaExpression)ChangeInputAndOutputNames(setDataExpression);
            sb.Append(serializer.Serialize(setDataExpression.ToExpressionSlim()));
        }
        var data = Encoding.Unicode.GetBytes(sb.ToString());
        Hash = MD5.HashData(data);
    }

    private System.Linq.Expressions.Expression ChangeInputAndOutputNames(LambdaExpression expression)
    {
        var changeParametersVisitor = new GenericVisitor();
        var inputArg = Parameter(expression.Parameters[0].Type, "input");
        var outputArg = Parameter(expression.Parameters[1].Type, "output");
        changeParametersVisitor.OnVisitParamter(ChangeParameterName);
        return changeParametersVisitor.Visit(expression);
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
