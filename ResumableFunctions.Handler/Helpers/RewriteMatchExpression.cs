using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Xml.Linq;
using FastExpressionCompiler;
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
        wait.PartialMatchValue = new IdsObjectVisitor(Result).Result;
        WaitMatchValue = (JObject)wait.PartialMatchValue;
        JsonResult = new UseJObjectVisitor(Result).Result;
    }

    public LambdaExpression Result { get; protected set; }
    public LambdaExpression JsonResult { get; protected set; }
    internal JObject WaitMatchValue { get; }


    protected override Expression VisitParameter(ParameterExpression node)
    {
        var isOutput = node == _wait.MatchIfExpression.Parameters[1];
        if (isOutput) return _outputArg;

        var isInput = node == _wait.MatchIfExpression.Parameters[0];
        if (isInput) return _inputArg;

        return base.VisitParameter(node);
    }

    private class UseJObjectVisitor : ExpressionVisitor
    {
        private LambdaExpression expression;
        private readonly ParameterExpression _pushedCall;

        public LambdaExpression Result { get; internal set; }

        public UseJObjectVisitor(LambdaExpression expression)
        {
            this.expression = expression;
            _pushedCall = Parameter(typeof(JObject), "pushedCall");


            var updatedBoy = Visit(expression.Body);
            var functionType = typeof(Func<,>)
                .MakeGenericType(
                    typeof(JObject),
                    typeof(bool));

            Result = Lambda(
                functionType,
                updatedBoy,
                _pushedCall);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Type.IsConstantType())
            {
                var isOutput = node == expression.Parameters[1];
                var castMethod = typeof(JToken).GetMethods().First(x => x.Name == "op_Explicit" && x.ReturnType == node.Type);
                return
                    Convert(
                            Property(_pushedCall, typeof(JObject).GetProperty("Item", new[] { typeof(string) }),
                            Constant(isOutput ? "output" : "input")
                        ),
                        node.Type,
                        castMethod);
            }
            return base.VisitParameter(node);
        }

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
                  matchExpression.Parameters[2]).CompileFast();
            try
            {
                result = getExp.DynamicInvoke(null, null, functionInstance);
                //todo:Must Handle cases enum,datetime,byte array,guid,or contract type in shared Dll
                //may use json serialization if type in shared Dll
                if (result != null)
                {
                    if (result.GetType().IsConstantType())
                    {
                        return Constant(result);
                    }
                    else if (result is DateTime date)
                    {
                        return New(typeof(DateTime).GetConstructor(new[] { typeof(long) }),Constant(date.Ticks));
                    }
                }
                throw new NotSupportedException($"Can't use expression `{expression}` in match. ");
            }
            catch
            {
                throw new NotSupportedException(message: $"Can't use expression `{expression}` in match. ");
            }
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
            if (!Result)
            {
                Result = true;
                return node;
            }
            return base.VisitParameter(node);
        }
    }
    private class IdsObjectVisitor : ExpressionVisitor
    {
        public JObject Result { get; } = new JObject();
        public IdsObjectVisitor(LambdaExpression matchExpression)
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