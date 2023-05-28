using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.InOuts;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Helpers;


public class RewriteMatchExpression : ExpressionVisitor
{
    private readonly ParameterExpression _functionInstanceArg;
    private readonly ParameterExpression _inputArg;
    private readonly ParameterExpression _outputArg;
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
        //   return (bool)check.DynamicInvoke(pushedCall.Input, pushedCall.Output, methodWait.CurrentFunction);
        _wait = wait;
        var expression = wait.MatchIfExpression;
        _functionInstanceArg = Parameter(wait.CurrentFunction.GetType(), "functionInstance");
        _inputArg = Parameter(expression.Parameters[0].Type, "input");
        _outputArg = Parameter(expression.Parameters[1].Type, "output");

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
            _inputArg,
            _outputArg,
            _functionInstanceArg);
        wait.PartialMatchValue = new WaitMatchValueGetter(Result).Result;
    }

    public LambdaExpression Result { get; protected set; }
    public override Expression Visit(Expression node)
    {
        return base.Visit(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {


        var isOutput = node == _wait.MatchIfExpression.Parameters[1];
        if (isOutput) return _outputArg;

        var isInput = node == _wait.MatchIfExpression.Parameters[0];
        if (isInput) return _inputArg;

        return base.VisitParameter(node);
    }

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
        else if (IsParamterAccess(node))
        {
            return base.VisitMember(node);
        }
        else
            throw new NotSupportedException(
                $"It's not support to access `{node}`," +
                $"only input, output, and resumable function isntance are allowed to be use in match expression.");

        return base.VisitMember(node);
    }

    private bool IsParamterAccess(MemberExpression memberExpression)
    {
        while (memberExpression != null && memberExpression.Expression is MemberExpression me)
        {
            memberExpression = me;
        }
        return memberExpression.Expression is ParameterExpression;
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

    private class WaitMatchValueGetter : ExpressionVisitor
    {
        public JObject Result { get; } = new JObject();
        public WaitMatchValueGetter(LambdaExpression matchExpression)
        {
            Visit(matchExpression);
        }
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Equal)
            {
                if (node.Left is ConstantExpression ce)
                    Result[node.Right.ToString()] = JToken.FromObject(ce.Value);
                if (node.Right is ConstantExpression constantExpression)
                    Result[node.Left.ToString()] = JToken.FromObject(constantExpression.Value);
            }
            return base.VisitBinary(node);
        }
    }
}