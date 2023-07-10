using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FastExpressionCompiler;
using ResumableFunctions.Handler.Helpers;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Expressions;

public class ExpressionsHashCalculator : ExpressionVisitor
{
    private int _localValuePartsCount = 0;
    public byte[] FinalHash { get; private set; }
    public LambdaExpression MatchExpression { get; private set; }
    public LambdaExpression SetDataExpression { get; private set; }

    public ExpressionsHashCalculator(LambdaExpression matchExpression, LambdaExpression setDataExpression)
    {
        try
        {
            MatchExpression = matchExpression;
            SetDataExpression = setDataExpression;
            //CalcInitialHash();
            CalcLocalValueParts();
            CalcFinalHash();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void CalcLocalValueParts()
    {
        var changeComputedParts = new GenericVisitor();
        var localValueMethodInfo =
            typeof(ResumableFunction)
            .GetMethod(nameof(ResumableFunction.LocalValue))
            .GetGenericMethodDefinition();
        changeComputedParts.OnVisitMethodCall(OnVisitMethodCall);
        if (MatchExpression != null)
            MatchExpression = (LambdaExpression)changeComputedParts.Visit(MatchExpression);
        if (SetDataExpression != null)
            SetDataExpression = (LambdaExpression)changeComputedParts.Visit(SetDataExpression);


        Expression OnVisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.IsGenericMethod &&
                methodCallExpression.Method.GetGenericMethodDefinition() == localValueMethodInfo)
            {
                var arg =
                    Lambda<Func<object>>(Convert(methodCallExpression.Arguments[0], typeof(object)))
                        .CompileFast()
                        .Invoke();
                if (arg.CanBeConstant())//todo:DateTime and Guid
                {
                    _localValuePartsCount++;
                    return Constant(arg);
                }
                else
                    throw new Exception(
                        $"The local value expression `{ExpressionExtensions.ToCSharpString(methodCallExpression.Arguments[0])}` can't be be convertred to constant type.");
            }
            return base.VisitMethodCall(methodCallExpression);
        }
    }

    //private void CalcFinalHash()
    //{
    //    if (_localValuePartsCount == 0)
    //    {
    //        FinalHash = InitialHash; 
    //        return;
    //    }


    //    var sb = new StringBuilder();
    //    if (MatchExpression != null)
    //        sb.Append(ExpressionExtensions.ToCSharpString(MatchExpression));

    //    if (SetDataExpression != null)
    //        sb.Append(ExpressionExtensions.ToCSharpString(SetDataExpression));

    //    var data = Encoding.Unicode.GetBytes(sb.ToString());
    //    FinalHash = MD5.HashData(data);
    //}

    private void CalcInitialHash()
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
        InitialHash = MD5.HashData(data);
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
