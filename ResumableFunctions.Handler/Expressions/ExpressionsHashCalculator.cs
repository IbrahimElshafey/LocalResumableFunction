using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FastExpressionCompiler;
using ResumableFunctions.Handler.Helpers;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Expressions;

public class ExpressionsHashCalculator : ExpressionVisitor
{
    private int _localValuePartsCount;
    public byte[] Hash { get; private set; }
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
            CalcHash();
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
            typeof(ResumableFunctionsContainer)
            .GetMethod(nameof(ResumableFunctionsContainer.LocalValue))
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
                _localValuePartsCount++;
                if (arg.CanBeConstant())
                {
                    return Constant(arg);
                }
                else if (arg is DateTime date)
                {
                    return New(typeof(DateTime).GetConstructor(new[] { typeof(long) }), Constant(date.Ticks));
                }
                else if (arg is Guid guid)
                {
                    return New(typeof(Guid).GetConstructor(new[] { typeof(string) }), Constant(guid.ToString()));
                }
                else
                {
                    try
                    {
                        return Call(
                              typeof(JsonSerializer).GetMethod("Deserialize", 1, new[] { typeof(string), typeof(JsonSerializerOptions) }),
                              Constant(JsonSerializer.Serialize(arg)),
                              MakeMemberAccess(null, typeof(JsonSerializerOptions).GetProperty("Default"))
                            );
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                        $"The local value expression `{ExpressionExtensions.ToCSharpString(methodCallExpression.Arguments[0])}` can't be be convertred to embedded value.", ex);
                    }

                }
            }
            return base.VisitMethodCall(methodCallExpression);
        }
    }


    private void CalcHash()
    {
        var sb = new StringBuilder();
        if (MatchExpression != null)
        {
            MatchExpression = (LambdaExpression)ChangeInputAndOutputNames(MatchExpression);
            //sb.Append(ExpressionExtensions.ToCSharpString(MatchExpression));
            sb.Append(MatchExpression.ToString());
        }

        if (SetDataExpression != null)
        {
            SetDataExpression = (LambdaExpression)ChangeInputAndOutputNames(SetDataExpression);
            //sb.Append(ExpressionExtensions.ToCSharpString(SetDataExpression));
            sb.Append(SetDataExpression.ToString());
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
