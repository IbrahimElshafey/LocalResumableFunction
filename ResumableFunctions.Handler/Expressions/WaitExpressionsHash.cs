using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Expressions;

public class WaitExpressionsHash : ExpressionVisitor
{
    public byte[] Hash { get; internal set; }
    public LambdaExpression MatchExpression { get; internal set; }
    public LambdaExpression SetDataExpression { get; internal set; }

    public WaitExpressionsHash(LambdaExpression matchExpression, LambdaExpression setDataExpression)
    {
        try
        {
            var serializer = new ExpressionSerializer();
            var sb = new StringBuilder();
            if (matchExpression != null)
            {
                MatchExpression =
                    (LambdaExpression)ChangeInputAndOutputNames(matchExpression);
                sb.Append(matchExpression.ToCSharpString());
            }
            if (setDataExpression != null)
            {
                SetDataExpression =
                    (LambdaExpression)ChangeInputAndOutputNames(setDataExpression);
                sb.Append(setDataExpression.ToCSharpString());
            }
            var data = Encoding.Unicode.GetBytes(sb.ToString());
            Hash = MD5.HashData(data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private Expression ChangeInputAndOutputNames(LambdaExpression expression)
    {
        var changeParametersVisitor = new GenericVisitor();
        var inputArg = Parameter(expression.Parameters[0].Type, "input");
        var outputArg = Parameter(expression.Parameters[1].Type, "output");
        changeParametersVisitor.OnVisitParameter(ChangeParameterName);
        return changeParametersVisitor.Visit(expression);
        Expression ChangeParameterName(ParameterExpression node)
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
