using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FastExpressionCompiler;
using ResumableFunctions.Handler.Helpers;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Expressions;

public class WaitExpressionsHash : ExpressionVisitor
{
    public byte[] Hash { get; private set; }
    public LambdaExpression MatchExpression { get; private set; }
    public LambdaExpression SetDataExpression { get; private set; }

    public WaitExpressionsHash(LambdaExpression matchExpression, LambdaExpression setDataExpression)
    {
        try
        {
            MatchExpression = matchExpression;
            SetDataExpression = setDataExpression;
            CalcLocalValuePartsInMatchExpression();
            CalcHash();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void CalcLocalValuePartsInMatchExpression()
    {
        var changeComputedParts = new GenericVisitor();
        var localValueMethodInfo = 
            typeof(ResumableFunction)
            .GetMethod(nameof(ResumableFunction.LocalValue))
            .GetGenericMethodDefinition();
        changeComputedParts.OnVisitMethodCall(OnVisitMethodCall);
        MatchExpression = (LambdaExpression)changeComputedParts.Visit(MatchExpression);
        Expression OnVisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.IsGenericMethod &&
                methodCallExpression.Method.GetGenericMethodDefinition() == localValueMethodInfo)
            {
                var arg =
                    Lambda<Func<object>>(Convert(methodCallExpression.Arguments[0], typeof(object)))
                        .CompileFast()
                        .Invoke();
                return Constant(arg);
            }
            return base.VisitMethodCall(methodCallExpression);
        }
    }

    private void CalcHash()
    {
        var sb = new StringBuilder();
        if (MatchExpression != null)
        {
            MatchExpression =
                (LambdaExpression)ChangeInputAndOutputNames(MatchExpression);
            sb.Append(ExpressionExtensions.ToCSharpString(MatchExpression));
        }

        if (SetDataExpression != null)
        {
            SetDataExpression =
                (LambdaExpression)ChangeInputAndOutputNames(SetDataExpression);
            sb.Append(ExpressionExtensions.ToCSharpString(SetDataExpression));
        }

        var data = Encoding.Unicode.GetBytes(sb.ToString());
        Hash = MD5.HashData(data);
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
