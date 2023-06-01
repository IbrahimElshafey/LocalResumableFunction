using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Xml.Linq;
using FastExpressionCompiler;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.InOuts;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Helpers;

//todo: refactor this class
public class RewriteMatchExpression : ExpressionVisitor
{
    private readonly ParameterExpression _functionInstanceArg;
    private readonly ParameterExpression _inputArg;
    private readonly ParameterExpression _outputArg;
    private readonly MethodWait _wait;
    public LambdaExpression MatchExpression { get; protected set; }

    public RewriteMatchExpression(MethodWait wait)
    {
        if (wait?.MatchIfExpression == null)
            return;
        if (wait.MatchIfExpression?.Parameters.Count == 3)
        {
            MatchExpression = wait.MatchIfExpression;
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
        MatchExpression = Lambda(
            functionType,
            updatedBoy.Body,
            _inputArg,
            _outputArg,
            _functionInstanceArg);
        var translateConstantsVistor = new TranslateConstantsVistor(MatchExpression, wait.CurrentFunction);
        MatchExpression = translateConstantsVistor.Result;
        wait.IdsObjectValue = new IdsObjectVisitor(MatchExpression, translateConstantsVistor.ConstantParts).Result;
        //WaitMatchValue = (JObject)wait.IdsObjectValue;
        //MatchExpressionWithJson = new MatchUsingJsonVisitor(MatchExpression).Result;
    }



    //public LambdaExpression MatchExpressionWithJson { get; protected set; }
    //internal JObject WaitMatchValue { get; }


    protected override Expression VisitParameter(ParameterExpression node)
    {
        //rename output
        var isOutput = node == _wait.MatchIfExpression.Parameters[1];
        if (isOutput) return _outputArg;

        //rename input
        var isInput = node == _wait.MatchIfExpression.Parameters[0];
        if (isInput) return _inputArg;

        return base.VisitParameter(node);
    }

    private class MatchUsingJsonVisitor : ExpressionVisitor
    {
        private LambdaExpression expression;
        private readonly ParameterExpression _pushedCall;

        public LambdaExpression Result { get; internal set; }

        public MatchUsingJsonVisitor(LambdaExpression expression)
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
        internal List<(Expression CallProp, Expression Calculated)> ConstantParts { get; } = new();
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
            if (left.IsCalculated && right.IsCalculated) ;//ignore
            else if (left.IsCalculated)
                ConstantParts.Add((right.Result, left.Result));
            else if (right.IsCalculated)
                ConstantParts.Add((left.Result, right.Result));
            return base.VisitBinary(MakeBinary(node.NodeType, left.Result, right.Result));
        }

        private (Expression Result, bool IsCalculated) TryTranslateToConstant(Expression expression)
        {
            if (new UseParamterVisitor(expression).IsParameterUsed) return (expression, false);


            var functionType =
                typeof(Func<,,,>)
                .MakeGenericType(
                    matchExpression.Parameters[0].Type,
                    matchExpression.Parameters[1].Type,
                    matchExpression.Parameters[2].Type,
                    typeof(object));

            var getExpValue = Lambda(
                functionType,
                Convert(expression, typeof(object)),
                  matchExpression.Parameters[0],
                  matchExpression.Parameters[1],
                  matchExpression.Parameters[2]).CompileFast();
            try
            {
                object result = getExpValue.DynamicInvoke(null, null, functionInstance);
                if (result != null)
                {
                    if (result.GetType().IsConstantType())
                    {
                        return (Constant(result), true);
                    }
                    //else if (expression.NodeType == ExpressionType.New)
                    //    return expression;
                    else if (result is DateTime date)
                    {
                        return (New(typeof(DateTime).GetConstructor(new[] { typeof(long) }), Constant(date.Ticks)), true);
                    }
                    else if (result is Guid guid)
                    {
                        return (New(
                            typeof(Guid).GetConstructor(new[] { typeof(string) }),
                            Constant(guid.ToString())), true);
                    }
                    else if (JsonConvert.SerializeObject(result) is string json)
                    {
                        return (Convert(
                            Call(
                                typeof(JsonConvert).GetMethod("DeserializeObject", 0, new[] { typeof(string), typeof(Type) }),
                                Constant(json),
                                Constant(result.GetType(), typeof(Type))
                            ),
                            result.GetType()
                        ), true);
                    }
                }
                throw new NotSupportedException($"Can't use expression `{expression}` in match. ");
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(message:
                    $"Can't use expression `{expression}` in match.\n" +
                    $"Exception: {ex}");
            }
        }

    }

    private class UseParamterVisitor : ExpressionVisitor
    {
        public bool IsParameterUsed { get; private set; }
        public UseParamterVisitor(Expression expression)
        {
            Visit(expression);
        }
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (!IsParameterUsed)
            {
                IsParameterUsed = true;
                return node;
            }
            return base.VisitParameter(node);
        }
    }

    private class IdsObjectVisitor : ExpressionVisitor
    {
        private readonly LambdaExpression _matchExpression;
        public List<(Expression CallProp, Expression Calculated)> ConstantParts { get; }

        public JObject Result { get; } = new JObject();
        public IdsObjectVisitor(LambdaExpression matchExpression, List<(Expression CallProp, Expression Calculated)> parts)
        {
            _matchExpression = matchExpression;
            ConstantParts = parts;
            Visit(matchExpression);
        }
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Equal)
            {

                if (ConstantParts.Any(x => x.Calculated == node.Left))
                {
                    bool isMandatory = new CheckMandatoryVistor(_matchExpression, ConstantParts, node.Right).IsMandatory;
                    if (isMandatory)
                        Result[node.Right.ToString()] = JToken.FromObject(GetNodeValue(node.Left));
                }

                if (ConstantParts.Any(x => x.Calculated == node.Right))
                {
                    var isMandatory = new CheckMandatoryVistor(_matchExpression, ConstantParts, node.Left).IsMandatory;
                    if (isMandatory)
                        Result[node.Left.ToString()] = JToken.FromObject(GetNodeValue(node.Right));
                }

            }
            return base.VisitBinary(node);
        }
        private object GetNodeValue(Expression expression)
        {
            if (expression is ConstantExpression constantExpression)
                return constantExpression.Value;
            //todo:get other objects values
            return null;
        }
    }

    private class CheckMandatoryVistor : ExpressionVisitor
    {
        private LambdaExpression _matchExpression;
        private readonly List<(Expression CallProp, Expression Calculated)> _parts;
        private Expression _partToCheck;
        public bool IsMandatory { get; internal set; }

        public CheckMandatoryVistor(
            LambdaExpression matchExpression,
            List<(Expression CallProp, Expression Calculated)> parts,
            Expression partToCheck)
        {
            //var x = Constant(15) == partToCheck;
            //x = Constant(19) == partToCheck;
            if (partToCheck.NodeType == ExpressionType.Parameter || partToCheck.NodeType == ExpressionType.MemberAccess)
            {
                _matchExpression = matchExpression;
                this._parts = parts;
                _partToCheck = partToCheck;
                var expression = Visit(matchExpression.Body);
                var compiled = Lambda<Func<bool>>(expression).CompileFast();
                IsMandatory = !compiled();
            }
        }
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Equal && (node.Left == _partToCheck || node.Right == _partToCheck))
                return Constant(false);
            var canBeTranslated = _parts.Any(x => x.Calculated == node.Left || x.Calculated == node.Right);
            if (canBeTranslated) return Constant(true);
            return base.VisitBinary(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
                return Constant(true);
            return base.VisitUnary(node);
        }
    }


    private class ConstantPart
    {
        public Expression PropPathExpression { get; set; }
        public Expression ConstantExpression { get; set; }
        public object ConstantValue { get; set; }
    }
}

public class GenericVisitor : ExpressionVisitor
{
    private List<VisitNodeFunction> _visitors = new();
    public void AddVisitor(
        Func<Expression, bool> visitCondition,
        Func<Expression, Expression> visitFunction)
    {
        _visitors.Add(new VisitNodeFunction(visitCondition, visitFunction));
    }

    public void ClearVisitors() => _visitors.Clear();

    public override Expression Visit(Expression node)
    {
        foreach (var visitor in _visitors)
        {
            if (visitor.VisitCondition(node))
                return base.Visit(visitor.VisitFunction(node));
        }
        return base.Visit(node);
    }
    private class VisitNodeFunction
    {
        public VisitNodeFunction(Func<Expression, bool> visitCondition, Func<Expression, Expression> visitFunction)
        {
            VisitCondition = visitCondition;
            VisitFunction = visitFunction;
        }
        public Func<Expression, bool> VisitCondition { get; }
        public Func<Expression, Expression> VisitFunction { get; }
    }
}

public class MatchNewVisitor : GenericVisitor
{
    public LambdaExpression MatchExpression { get; private set; }
    private ParameterExpression _functionInstanceArg;
    private ParameterExpression _inputArg;
    private ParameterExpression _outputArg;
    private object _currentFunctionInstance;

    public MatchNewVisitor(LambdaExpression matchExpression, object instance)
    {
        MatchExpression = matchExpression;
        if (MatchExpression == null)
            return;
        if (MatchExpression?.Parameters.Count == 3)
        {
            MatchExpression = MatchExpression;
            return;
        }
        _currentFunctionInstance = instance;
        ChangeInputOutputParamsNames();
    }

    private void ChangeInputOutputParamsNames()
    {
        var expression = MatchExpression;
        _functionInstanceArg = Parameter(_currentFunctionInstance.GetType(), "functionInstance");
        _inputArg = Parameter(expression.Parameters[0].Type, "input");
        _outputArg = Parameter(expression.Parameters[1].Type, "output");
        AddVisitor((expression) => expression is ParameterExpression, ChangeParameters);
        var updatedBoy = (LambdaExpression)Visit(MatchExpression);
        var functionType = typeof(Func<,,,>)
            .MakeGenericType(
                updatedBoy.Parameters[0].Type,
                updatedBoy.Parameters[1].Type,
                _currentFunctionInstance.GetType(),
                typeof(bool));

        MatchExpression = Lambda(
            functionType,
            updatedBoy.Body,
            _inputArg,
            _outputArg,
            _functionInstanceArg);
    }

    protected Expression ChangeParameters(Expression expression)
    {
        if (expression is ParameterExpression node)
        {
            var isOutput = node == MatchExpression.Parameters[1];
            if (isOutput) return _outputArg;

            //rename input
            var isInput = node == MatchExpression.Parameters[0];
            if (isInput) return _inputArg;

        }
        return base.Visit(expression);
    }

}
