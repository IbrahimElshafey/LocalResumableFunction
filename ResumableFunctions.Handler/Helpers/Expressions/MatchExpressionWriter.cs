using FastExpressionCompiler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Helpers.Expressions;
public partial class MatchExpressionWriter : ExpressionVisitor
{
    private LambdaExpression _matchExpression;
    private ParameterExpression _functionInstanceArg;
    private ParameterExpression _inputArg;
    private ParameterExpression _outputArg;
    private object _currentFunctionInstance;
    private List<ConstantPart> _constantParts = new();

    public LambdaExpression MatchExpression { get; private set; }
    public LambdaExpression CallMandatoryPartExpression { get; private set; }
    public Expression<Func<ExpandoObject, string[]>> CallMandatoryPartExpressionDynamic { get; private set; }
    public LambdaExpression InstanceMandatoryPartExpression { get; private set; }
    public bool IsMandatoryPartFullMatch { get; private set; }

    /* NEW expressions
    - MatchExpressionDynamic
    - CallMandatoryPartExpression
    - CallMandatoryPartExpressionDynamic
    - InstanceMandatoryPartExpression
    - InstanceMandatoryPartExpressionDynamic
    */

    public MatchExpressionWriter(LambdaExpression matchExpression, object instance)
    {
        _matchExpression = matchExpression;
        if (_matchExpression == null)
            return;
        if (_matchExpression?.Parameters.Count == 3)
        {
            MatchExpression = _matchExpression;
            return;
        }
        _currentFunctionInstance = instance;
        ChangeInputOutputParamsNames();
        CalcConstantInExpression();
        //GenerateMatchUsingJson();
        MarkMandatoryConstants();

        if (_constantParts.Any(x => x.IsMandatory))
        {
            var mandatoryPartVistor = new MandatoryPartVistor(_matchExpression, _constantParts);
            CallMandatoryPartExpression = mandatoryPartVistor.CallMandatoryPartExpression;
            CallMandatoryPartExpressionDynamic = mandatoryPartVistor.CallMandatoryPartExpressionDynamic;
            InstanceMandatoryPartExpression = mandatoryPartVistor.InstanceMandatoryPartExpression;
            SetIsMandatoryPartFullMatchValue();
        }
    }

    private void ChangeInputOutputParamsNames()
    {
        var expression = _matchExpression;
        _functionInstanceArg = Parameter(_currentFunctionInstance.GetType(), "functionInstance");
        _inputArg = Parameter(expression.Parameters[0].Type, "input");
        _outputArg = Parameter(expression.Parameters[1].Type, "output");
        var changeParameterVistor = new GenericVisitor();
        changeParameterVistor.OnVisitParamter(ChangeParameters);
        changeParameterVistor.OnVisitConstant(OnVisitFunctionInstance);
        var updatedBoy = (LambdaExpression)changeParameterVistor.Visit(_matchExpression);
        var functionType = typeof(Func<,,,>)
            .MakeGenericType(
                updatedBoy.Parameters[0].Type,
                updatedBoy.Parameters[1].Type,
                _currentFunctionInstance.GetType(),
                typeof(bool));

        _matchExpression = Lambda(
            functionType,
            updatedBoy.Body,
            _inputArg,
            _outputArg,
            _functionInstanceArg);

        Expression ChangeParameters(ParameterExpression node)
        {
            //rename output
            var isOutput = node == _matchExpression.Parameters[1];
            if (isOutput) return _outputArg;

            //rename input
            var isInput = node == _matchExpression.Parameters[0];
            if (isInput) return _inputArg;

            return base.VisitParameter(node);
        }

        Expression OnVisitFunctionInstance(ConstantExpression node)
        {
            if (node.Value == _currentFunctionInstance)
                return _functionInstanceArg;
            return base.VisitConstant(node);
        }
    }

    private void CalcConstantInExpression()
    {
        MatchExpression = _matchExpression;
        var constantTranslationVisior = new GenericVisitor();
        constantTranslationVisior.OnVisitBinary(TryEvaluateBinaryParts);
        constantTranslationVisior.OnVisitUnary(VisitNotEqual);
        //aaa
        _matchExpression = (LambdaExpression)constantTranslationVisior.Visit(_matchExpression);

        Expression TryEvaluateBinaryParts(BinaryExpression node)
        {
            var left = TryTranslateToConstant(node.Left);
            var right = TryTranslateToConstant(node.Right);
            if (left.IsCalculated && right.IsCalculated)
            {
                ;//ignore
            }
            else if (left.IsCalculated)
                _constantParts.Add(new(node.NodeType, right.Result, left.Result, left.Value, node.Left));
            else if (right.IsCalculated)
                _constantParts.Add(new(node.NodeType, left.Result, right.Result, right.Value, node.Right));

            //translate bool prop like `&& input.IsHappy` to `&& input.IsHappy == true `
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

            throw new NotSupportedException(
                $"Can't use expression `{expression}` in match because it's evaluated to `NULL`.");

        }

        bool IsUseParameters(Expression expression)
        {
            var checkUseParamter = new GenericVisitor();
            var isParamter = false;
            checkUseParamter.OnVisitParamter(param =>
            {
                if (param.Name == "input" || param.Name == "output")
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
                var functionType = typeof(Func<,>).MakeGenericType(_matchExpression.Parameters[2].Type, typeof(object));
                var getExpValue = Lambda(functionType, Convert(expression, typeof(object)), _matchExpression.Parameters[2]).CompileFast();
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

        foreach (var constPart in _constantParts)
        {
            if (constPart.IsMandatory || constPart.Operator != ExpressionType.Equal) continue;

            partToCheck = constPart.ConstantExpression;
            var expression = changeToBools.Visit(_matchExpression.Body);
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
    }

    private void SetIsMandatoryPartFullMatchValue()
    {
        var changeToBools = new GenericVisitor();
        changeToBools.OnVisitBinary(VisitBinary);
        var exp = changeToBools.Visit(_matchExpression.Body);
        var compiled = Lambda<Func<bool>>(exp).CompileFast();
        IsMandatoryPartFullMatch = compiled();
        Expression VisitBinary(BinaryExpression node)
        {
            var isMandatory =
                node.NodeType == ExpressionType.Equal &&
                _constantParts.Any(x => x.IsMandatory && (x.PropPathExpression == node.Left || x.PropPathExpression == node.Right));
            if (isMandatory)
                return Constant(true);
            var canBeTranslated = _constantParts.Any(x => x.ConstantExpression == node.Left || x.ConstantExpression == node.Right);
            if (canBeTranslated) return Constant(false);
            return base.VisitBinary(node);
        }
    }

    private void GenerateMatchUsingJson()
    {
        //var _pushedCall = Parameter(typeof(JObject), "pushedCall");
        //var usePushedCall = new GenericVisitor();
        //usePushedCall.OnVisitParamter(VisitParameter);
        //var updatedBoy = usePushedCall.Visit(MatchExpressionWithConstants.Body);
        //JObjectMatchExpression = Lambda<Func<JObject, bool>>(updatedBoy, _pushedCall);
        //Expression VisitParameter(ParameterExpression node)
        //{
        //    if (node.Type.IsConstantType())
        //    {
        //        var isOutput = node == MatchExpressionWithConstants.Parameters[1];
        //        var castMethod = typeof(JToken).GetMethods().First(x => x.Name == "op_Explicit" && x.ReturnType == node.Type);
        //        return
        //            Convert(
        //                    Property(_pushedCall, typeof(JObject).GetProperty("Item", new[] { typeof(string) }),
        //                    Constant(isOutput ? "output" : "input")
        //                ),
        //                node.Type,
        //                castMethod);
        //    }
        //    return base.VisitParameter(node);
        //}
    }


    private Expression AccesUsingJToken(Expression propPathExpression, ParameterExpression pushedCall)
    {
        var useJobject = new GenericVisitor();
        //useJobject.OnVisitParamter(VisitParameter);
        //useJobject.OnVisitMember(VisitMember);
        useJobject.AddVisitor(x => x is ParameterExpression || x is MemberExpression, VisitParameterOrMember);
        return useJobject.Visit(propPathExpression);

        Expression VisitParameterOrMember(Expression node)
        {
            var check = IsInputOrOutput(node);
            if (check.IsInput || check.IsOutput)
                return
                Convert(
                    Call(
                        Call(
                            pushedCall,
                            typeof(JToken).GetMethod("SelectToken", new[] { typeof(string) }),
                            Constant(node.ToString())
                        ),
                        typeof(JToken).GetMethod("ToObject", 0, new[] { typeof(Type) }),
                        Constant(
                            node.Type,
                            typeof(Type)
                        )
                    ),
                    node.Type
                );
            return base.Visit(node);

            //Call(
            //           typeof(JsonConvert).GetMethod("DeserializeObject", 0, new[] { typeof(string), typeof(Type) }),
            //           Constant(json),
            //           Constant(result.GetType(), typeof(Type))
            //)
        }

        (bool IsInput, bool IsOutput) IsInputOrOutput(Expression expression)
        {
            var checkUseParamter = new GenericVisitor();
            var isInput = false;
            var isOutput = false;
            checkUseParamter.OnVisitParamter(param =>
            {
                isInput = param == _matchExpression.Parameters[0] || isInput;
                isOutput = param == _matchExpression.Parameters[1] || isOutput;
                return param;
            });
            checkUseParamter.Visit(expression);
            return (isInput, isOutput);
        }
    }
}
