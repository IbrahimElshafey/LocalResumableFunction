using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
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
        Result = new TranslateConstantsVistor(Result, wait.CurrentFunction).Result;
        wait.PartialMatchValue = new WaitMatchValueVisitor(Result).Result;
        WaitMatchValue = (JObject)wait.PartialMatchValue;
    }

    public LambdaExpression Result { get; protected set; }
    internal JObject WaitMatchValue { get; }

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

    private class TranslateConstantsVistor : ExpressionVisitor
    {
        private readonly LambdaExpression matchExpression;
        private readonly object functionInstance;

        public LambdaExpression Result { get; }

        public TranslateConstantsVistor(LambdaExpression matchExpression, object functionInstance)
        {
            this.matchExpression = matchExpression;
            this.functionInstance = functionInstance;
            Result = (LambdaExpression)Visit(matchExpression);
        }


        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = TryTranslateToConstant(node.Left);
            var right = TryTranslateToConstant(node.Right);
            return base.VisitBinary(MakeBinary(node.NodeType, left, right));
        }

        private Expression TryTranslateToConstant(Expression expression)
        {
            if (new UseParamterVisitor(expression).Result) return expression;


            var functionType =
                typeof(Func<,,,>)
                .MakeGenericType(
                    matchExpression.Parameters[0].Type,
                    matchExpression.Parameters[1].Type,
                    matchExpression.Parameters[2].Type,
                    typeof(object));
            object result = null;
            var getExp = Lambda(
                functionType,
                Convert(expression, typeof(object)),
                  matchExpression.Parameters[0],
                  matchExpression.Parameters[1],
                  matchExpression.Parameters[2]).Compile();
            try
            {
                result = getExp.DynamicInvoke(null, null, functionInstance);
                if (IsBasicType(result))
                {
                    return Constant(result);
                }
                else
                {
                    //todo:throw
                    throw new NotSupportedException($"Can't translate `{expression}` to constant value. ");
                }

            }
            catch
            {
                throw new NotSupportedException(message: $"Can't translate `{expression.ToString()}` to constant value. ");
            }
        }



        protected bool IsBasicType(object result)
        {
            return result != null && (result.GetType().IsValueType || result.GetType() == typeof(string));
        }
    }
    private class UseParamterVisitor : ExpressionVisitor
    {
        public bool Result { get; private set; } = false;
        public UseParamterVisitor(Expression expression)
        {
            Visit(expression);
        }
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (Result == false)
            {
                Result = true;
                return node;
            }
            return base.VisitParameter(node);
        }
    }
    private class WaitMatchValueVisitor : ExpressionVisitor
    {
        private readonly LambdaExpression matchExpression;

        public JObject Result { get; } = new JObject();
        public WaitMatchValueVisitor(LambdaExpression matchExpression)
        {
            this.matchExpression = matchExpression;
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