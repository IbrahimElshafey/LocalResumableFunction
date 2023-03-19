using System.Linq.Expressions;
using ResumableFunctions.Core.InOuts;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Core.Helpers;

public class RewriteMatchExpression : ExpressionVisitor
{
    private readonly ParameterExpression _functionInstanceArg;
    private readonly MethodWait _wait;

    public RewriteMatchExpression(MethodWait wait)
    {
        if (wait?.MatchIfExpression == null)
            return;
        if (wait.MatchIfExpression?.Parameters.Count == 3)
        {
            Result = wait.MatchIfExpression;
            return;
        }
        //  .If((input, output) => output == true)
        //   return (bool)check.DynamicInvoke(pushedMethod.Input, pushedMethod.Output, methodWait.CurrentFunction);
        _wait = wait;
        _functionInstanceArg = Parameter(wait.CurrentFunction.GetType(), "functionInstance");

        var updatedBoy = (LambdaExpression)Visit(wait.MatchIfExpression);
        var functionType = typeof(Func<,,,>)
            .MakeGenericType(
                updatedBoy.Parameters[0].Type,
                updatedBoy.Parameters[1].Type,
                wait.CurrentFunction.GetType(),
                typeof(bool));
        Result = Lambda(
            functionType,
            updatedBoy.Body,
            updatedBoy.Parameters[0],
            updatedBoy.Parameters[1],
            _functionInstanceArg);
        //Result = (LambdaExpression)Visit(Result);
    }

    public LambdaExpression Result { get; protected set; }

    protected override Expression VisitMember(MemberExpression node)
    {
        //replace [FunctionClass].Data.Prop with [_dataParamter.Prop] or constant value
        var x = node.GetDataParamterAccess(_functionInstanceArg);
        if (x.IsFunctionData)
        {
            if (IsBasicType(node.Type))
            {
                var value = GetValue(x.NewExpression);
                if (value != null)
                    return Constant(value, node.Type);
            }

            _wait.NeedFunctionStateForMatch = true;
            return x.NewExpression;
        }

        return base.VisitMember(node);
    }


    protected bool IsBasicType(Type type)
    {
        return type.IsPrimitive || type == typeof(string);
    }


    protected object GetValue(MemberExpression node)
    {
        try
        {
            var getterLambda = Lambda(node, _functionInstanceArg);
            var getter = getterLambda.Compile();
            return getter?.DynamicInvoke(_wait.CurrentFunction);
        }
        catch (Exception)
        {
            //expected to be not null
            return null;
        }
    }
}