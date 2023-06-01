﻿using FastExpressionCompiler;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Handler.InOuts;
using System.Linq.Expressions;
using System.Xml.Linq;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Helpers;

public class MatchNewVisitor : ExpressionVisitor
{
    public LambdaExpression MatchExpression { get; private set; }
    public LambdaExpression MatchExpressionWithoutConstants { get; private set; }
    public Expression<Func<JObject, bool>> JObjectMatchExpression { get; private set; }
    public string RefineMatchModifier { get; private set; }

    private ParameterExpression _functionInstanceArg;
    private ParameterExpression _inputArg;
    private ParameterExpression _outputArg;
    private object _currentFunctionInstance;
    private List<ConstantPart> _constantParts = new();

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
        CalcConstantInExpression();
        //GenerateMatchUsingJson();
        MarkMandatoryConstants();
        //GenerateIdExtractorExpression();
        RefineMatchModifier = _constantParts
            .Where(x => x.IsMandatory)
            .OrderBy(x => x.PropPathExpression.ToString())
            .Select(x => x.Value.ToString())
            .Aggregate((x, y) => $"{x}#{y}");
    }

    private void ChangeInputOutputParamsNames()
    {
        var expression = MatchExpression;
        _functionInstanceArg = Parameter(_currentFunctionInstance.GetType(), "functionInstance");
        _inputArg = Parameter(expression.Parameters[0].Type, "input");
        _outputArg = Parameter(expression.Parameters[1].Type, "output");
        var changeParameterVistor = new GenericVisitor();
        changeParameterVistor.OnVisitParamter(ChangeParameters);
        var updatedBoy = (LambdaExpression)changeParameterVistor.Visit(MatchExpression);
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

        Expression ChangeParameters(ParameterExpression node)
        {
            //rename output
            var isOutput = node == MatchExpression.Parameters[1];
            if (isOutput) return _outputArg;

            //rename input
            var isInput = node == MatchExpression.Parameters[0];
            if (isInput) return _inputArg;

            return base.VisitParameter(node);
        }
    }

    private void CalcConstantInExpression()
    {
        MatchExpressionWithoutConstants = MatchExpression;
        var constantTranslationVisior = new GenericVisitor();
        constantTranslationVisior.OnVisitBinary(TryEvaluateBinaryParts);
        constantTranslationVisior.OnVisitUnary(VisitNotEqual);
        //aaa
        MatchExpression = (LambdaExpression)constantTranslationVisior.Visit(MatchExpression);

        Expression TryEvaluateBinaryParts(BinaryExpression node)
        {
            var left = TryTranslateToConstant(node.Left);
            var right = TryTranslateToConstant(node.Right);
            if (left.IsCalculated && right.IsCalculated) ;//ignore
            else if (left.IsCalculated)
                _constantParts.Add(new(node.NodeType, right.Result, left.Result, left.Value));
            else if (right.IsCalculated)
                _constantParts.Add(new(node.NodeType, left.Result, right.Result, right.Value));

            if (node.Left.Type == typeof(bool) &&
                IsUseParameters(node.Left) &&
                (node.Left is ParameterExpression || node.Left is MemberExpression) &&
                node.Right is not ConstantExpression)
                left.Result = MakeBinary(ExpressionType.Equal, node.Left, Constant(true));
            if (node.Right.Type == typeof(bool) &&
                IsUseParameters(node.Right) &&
                (node.Right is ParameterExpression || node.Right is MemberExpression) &&
                node.Left is not ConstantExpression)
                right.Result = MakeBinary(ExpressionType.Equal, node.Right, Constant(true));

            return base.VisitBinary(MakeBinary(node.NodeType, left.Result, right.Result));
        }

        Expression VisitNotEqual(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not &&
                IsUseParameters(node.Operand) &&
                (node.Operand is MemberExpression || node.Operand is ParameterExpression))
            {
                return TryEvaluateBinaryParts(MakeBinary(ExpressionType.Equal, node.Operand, Constant(false)));
            }
            return base.VisitUnary(node);
        }

        (Expression Result, bool IsCalculated, object Value) TryTranslateToConstant(Expression expression)
        {

            if (IsUseParameters(expression)) return (expression, false, null);
            var result = GetExpressionValue(expression);
            if (result != null)
                return ObjectAsConst(result);
            throw new NotSupportedException($"Can't use expression `{expression}` in match because it's evaluated to `NULL`.");

        }

        bool IsUseParameters(Expression expression)
        {
            var checkUseParamter = new GenericVisitor();
            var isParamter = false;
            checkUseParamter.OnVisitParamter(param =>
            {
                isParamter = true || isParamter;
                return param;
            });
            checkUseParamter.Visit(expression);
            return isParamter;
        }

        object GetExpressionValue(Expression expression)
        {
            try
            {
                var functionType = typeof(Func<,>).MakeGenericType(MatchExpression.Parameters[2].Type, typeof(object));
                var getExpValue = Lambda(functionType, Convert(expression, typeof(object)), MatchExpression.Parameters[2]).CompileFast();
                return getExpValue.DynamicInvoke(_currentFunctionInstance);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(message:
                    $"Can't use expression `{expression}` in match because we can't compute its value.\n" +
                    $"Exception: {ex}");
            }
        }

        (Expression Result, bool IsCalculated, object Value) ObjectAsConst(object result)
        {
            if (result.GetType().IsConstantType())
            {
                return (Constant(result), true, result);
            }
            //else if (expression.NodeType == ExpressionType.New)
            //    return expression;
            else if (result is DateTime date)
            {
                return (New(typeof(DateTime).GetConstructor(new[] { typeof(long) }), Constant(date.Ticks)), true, date);
            }
            else if (result is Guid guid)
            {
                return (New(
                    typeof(Guid).GetConstructor(new[] { typeof(string) }),
                    Constant(guid.ToString())), true, guid);
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
                ), true, json);
            }
            throw new NotSupportedException(message:
                   $"Can't use value `{result}` in match because it type can't be serialized.\n");
        }
    }

    //not,member,paramter
    private void MarkMandatoryConstants()
    {
        var changeToBools = new GenericVisitor();
        Expression partToCheck = null;
        changeToBools.OnVisitBinary(VisitBinary);
        //changeToBools.OnVisitUnary(VisitUnary);
        //changeToBools.OnVisitMember(VisitMember);
        //changeToBools.OnVisitParamter(VisitParameters);
        foreach (var constPart in _constantParts)
        {
            if (constPart.IsMandatory || constPart.Operator != ExpressionType.Equal) continue;

            partToCheck = constPart.ConstantExpression;
            var expression = changeToBools.Visit(MatchExpression.Body);
            var compiled = Lambda<Func<bool>>(expression).CompileFast();
            constPart.IsMandatory = !compiled();
        }

        Expression VisitBinary(BinaryExpression node)
        {
            if (node.Left == partToCheck || node.Right == partToCheck)
                return Constant(false);
            var canBeTranslated = _constantParts.Any(x => x.ConstantExpression == node.Left || x.ConstantExpression == node.Right);
            if (canBeTranslated) return Constant(true);
            return base.VisitBinary(node);
        }

        //Expression VisitMember(MemberExpression memberExpression)
        //{
        //    if (memberExpression.Type == typeof(bool))
        //        return Constant(true);
        //    return base.VisitMember(memberExpression);
        //}
        //Expression VisitParameter(ParameterExpression parameterExpression)
        //{
        //    if (parameterExpression.Type == typeof(bool))
        //        return Constant(true);
        //    return base.VisitParameter(parameterExpression);
        //}
    }

    private void GenerateMatchUsingJson()
    {
        var _pushedCall = Parameter(typeof(JObject), "pushedCall");
        var usePushedCall = new GenericVisitor();
        usePushedCall.OnVisitParamter(VisitParameterq);
        var updatedBoy = usePushedCall.Visit(MatchExpression.Body);
        JObjectMatchExpression = Lambda<Func<JObject, bool>>(updatedBoy, _pushedCall);
        Expression VisitParameterq(ParameterExpression node)
        {
            if (node.Type.IsConstantType())
            {
                var isOutput = node == MatchExpression.Parameters[1];
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

    private class ConstantPart
    {
        public ConstantPart(ExpressionType op, Expression propPathExpression, Expression constantExpression, object value)
        {
            PropPathExpression = propPathExpression;
            ConstantExpression = constantExpression;
            Value = value;
            Operator = op;
        }

        public Expression PropPathExpression { get; set; }
        public Expression ConstantExpression { get; set; }
        public ExpressionType Operator { get; set; }
        public object Value { get; set; }
        public bool IsMandatory { get; set; }
    }
}
