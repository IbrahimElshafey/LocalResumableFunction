using System.Linq.Expressions;
using LocalResumableFunction.InOuts;
using static System.Linq.Expressions.Expression;

namespace LocalResumableFunction.Helpers;

public class RewriteMatchExpression : ExpressionVisitor
{
    private readonly ParameterExpression _functionInstanceArg;
    private readonly MethodWait _wait;

    public RewriteMatchExpression(MethodWait wait)
    {
        _wait = wait;
        _functionInstanceArg = Parameter(wait.CurrntFunction.GetType(), "functionInstance");

        var updatedBoy = (LambdaExpression)Visit(wait.MatchIfExpression);
        var functionType = typeof(Func<,,>)
            .MakeGenericType(wait.CurrntFunction.GetType(), updatedBoy.Parameters[0].Type, typeof(bool));
        Result = Lambda(functionType, updatedBoy.Body, _functionInstanceArg, updatedBoy.Parameters[0]);
        //Result = (LambdaExpression)Visit(Result);
    }

    public LambdaExpression Result { get; }


    //protected override Expression VisitConstant(ConstantExpression node)
    //{
    //    if (node.Type == _wait.CurrntFunction.GetType())
    //        return _functionInstanceArg;
    //    return base.VisitConstant(node);
    //}

    protected override Expression VisitMember(MemberExpression node)
    {
        //replace [FunctionClass].Data.Prop with [_dataParamter.Prop] or constant value
        var x = node.GetDataParamterAccess(_functionInstanceArg);
        if (x.IsFunctionData)
        {
            if (IsBasicType(node.Type))
                return Constant(GetValue(x.NewExpression), node.Type);
            _wait.NeedFunctionStateForMatch = true;
            return x.NewExpression;
        }

        return base.VisitMember(node);
    }


    private bool IsBasicType(Type type)
    {
        return type.IsPrimitive || type == typeof(string);
    }


    private object GetValue(MemberExpression node)
    {
        var getterLambda = Lambda(node, _functionInstanceArg);
        var getter = getterLambda.Compile();
        return getter?.DynamicInvoke(_wait.CurrntFunction);
    }
}