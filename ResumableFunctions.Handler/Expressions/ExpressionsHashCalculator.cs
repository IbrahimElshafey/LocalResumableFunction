using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FastExpressionCompiler;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Expressions;

public class ExpressionsHashCalculator : ExpressionVisitor
{
    private int _localValuePartsCount;
    public byte[] Hash { get; private set; }
    public LambdaExpression MatchExpression { get; private set; }
    public MethodData SetDataCall { get; private set; }
    public MethodData CancelMethodData { get; }

    public ExpressionsHashCalculator(LambdaExpression matchExpression, MethodData setDataCall, MethodData cancelMethod)
    {
        try
        {
            MatchExpression = matchExpression;
            SetDataCall = setDataCall;
            CancelMethodData = cancelMethod;
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
        var localValueWrapper =
            typeof(ResumableFunctionsContainer)
            .GetMethod(nameof(ResumableFunctionsContainer.LocalValue))
            .GetGenericMethodDefinition();
        changeComputedParts.OnVisitMethodCall(OnVisitMethodCall);
        if (MatchExpression != null)
            MatchExpression = (LambdaExpression)changeComputedParts.Visit(MatchExpression);
        //if (SetDataCall != null)
        //    SetDataCall = (LambdaExpression)changeComputedParts.Visit(SetDataCall);


        Expression OnVisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.IsGenericMethod &&
                methodCallExpression.Method.GetGenericMethodDefinition() == localValueWrapper)
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
                    return SerializeValue(methodCallExpression, arg);
                }
            }
            return base.VisitMethodCall(methodCallExpression);
        }
    }

    private static Expression SerializeValue(MethodCallExpression methodCallExpression, object arg)
    {
        try
        {
            return
                Call(
                    typeof(JsonSerializer).GetMethod("Deserialize", 1, new[] { typeof(string), typeof(JsonSerializerOptions) }).MakeGenericMethod(arg.GetType()),
                    Constant(JsonSerializer.Serialize(arg)),
                    MakeMemberAccess(null,
                        typeof(JsonSerializerOptions).GetProperty("Default")
                    )
                );
        }
        catch (Exception ex)
        {
            throw new Exception(
            $"The local value expression `{ExpressionExtensions.ToCSharpString(methodCallExpression.Arguments[0])}` can't be be convertred to embedded value.", ex);
        }
    }

    private void CalcHash()
    {
        var sb = new StringBuilder();
        if (MatchExpression != null)
        {
            MatchExpression = (LambdaExpression)ChangeInputAndOutputNames(MatchExpression);
            sb.Append(MatchExpression.ToString());
        }

        //if (SetDataCall != null)
        //{
        //    SetDataCall = (LambdaExpression)ChangeInputAndOutputNames(SetDataCall);
        //    sb.Append(SetDataCall.ToString());
        //}

        var data = Encoding.Unicode.GetBytes(sb.ToString()).ToList();
        
        if (CancelMethodData?.MethodHash != null)
            data.AddRange(CancelMethodData.MethodHash);

        if (SetDataCall?.MethodHash != null)
            data.AddRange(SetDataCall.MethodHash);

        Hash = MD5.HashData(data.ToArray());
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
