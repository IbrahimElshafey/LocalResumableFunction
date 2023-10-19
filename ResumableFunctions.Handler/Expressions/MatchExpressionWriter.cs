using System.Linq.Expressions;
using FastExpressionCompiler;
using ResumableFunctions.Handler.Helpers;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Expressions;
public partial class MatchExpressionWriter : ExpressionVisitor
{
    private LambdaExpression _matchExpression;
    private readonly object _currentFunctionInstance;
    private readonly List<ExpressionPart> _expressionParts = new();


    public MatchExpressionParts MatchExpressionParts { get; }
    public MatchExpressionWriter(LambdaExpression matchExpression, object functionInstance)
    {
        MatchExpressionParts = new();
        _matchExpression = matchExpression;
        if (_matchExpression == null)
            return;
        if (_matchExpression?.Parameters.Count == 4)
        {
            MatchExpressionParts.MatchExpression = _matchExpression;
            return;
        }
        _currentFunctionInstance = functionInstance;

        ChangeSignature();
        FindExactMatchParts();
        MarkMandatoryParts();
        ReplaceLocalVariables();
        if (_expressionParts.Any(x => x.IsMandatory))
        {
            var mandatoryPartVisitor = new MandatoryPartExpressionsGenerator(_matchExpression, _expressionParts);
            MatchExpressionParts.CallMandatoryPartExpression = mandatoryPartVisitor.CallMandatoryPartExpression;
            MatchExpressionParts.InstanceMandatoryPartExpression = mandatoryPartVisitor.InstanceMandatoryPartExpression;
            SetIsMandatoryPartFullMatchValue();
        }
    }

    private void ReplaceLocalVariables()
    {
        var changeClosureVarsVisitor = new GenericVisitor();
        Expression closure = null;
        changeClosureVarsVisitor.OnVisitConstant(node =>
        {
            if (node.Type.Name.StartsWith(Constants.CompilerClosurePrefix))
            {
                closure = node;
                return _matchExpression.Parameters[3];
            }
            return base.VisitConstant(node);
        });
        MatchExpressionParts.MatchExpression = (LambdaExpression)changeClosureVarsVisitor.Visit(MatchExpressionParts.MatchExpression);
        if (closure != null)
        {
            var value = Lambda<Func<object>>(closure).CompileFast().Invoke();
            if (value != null)
                MatchExpressionParts.Closure = value;
        }
    }

    private void ChangeSignature()
    {
        var expression = _matchExpression;
        var inputArg = Parameter(_matchExpression.Parameters[0].Type, "input");
        var outputArg = Parameter(_matchExpression.Parameters[1].Type, "output");
        var functionInstanceArg = Parameter(_currentFunctionInstance.GetType(), "functionInstance");
        var closureType = GetClosureType();
        var localVarsArg = Parameter(closureType, "closure");
        var changeParameterVisitor = new GenericVisitor();
        changeParameterVisitor.OnVisitConstant(OnVisitConstant);
        changeParameterVisitor.OnVisitParameter(OnVisitParameter);
        var updatedBoy = changeParameterVisitor.Visit(_matchExpression.Body);
        var functionType = typeof(Func<,,,,>)
            .MakeGenericType(
                inputArg.Type,
                outputArg.Type,
                _currentFunctionInstance.GetType(),
                closureType,
                typeof(bool));

        _matchExpression = Lambda(
            functionType,
            updatedBoy,
            inputArg,
            outputArg,
            functionInstanceArg,
            localVarsArg
            );

        Expression OnVisitConstant(ConstantExpression node)
        {
            if (node.Value == _currentFunctionInstance)
                return functionInstanceArg;
            return base.VisitConstant(node);
        }
        Expression OnVisitParameter(ParameterExpression parameter)
        {
            if (parameter == _matchExpression.Parameters[0])
                return inputArg;
            if (parameter == _matchExpression.Parameters[1])
                return outputArg;
            return base.VisitParameter(parameter);
        }
    }

    private Type GetClosureType()
    {
        Type result = null;
        var getClosureTypeVisitor = new GenericVisitor();
        getClosureTypeVisitor.OnVisitConstant(OnVisitConstant);
        getClosureTypeVisitor.StopWhen(_ => result != null);
        getClosureTypeVisitor.Visit(_matchExpression);
        Expression OnVisitConstant(ConstantExpression node)
        {
            if (node.Type.Name.StartsWith(Constants.CompilerClosurePrefix))
                result = node.Type;
            return base.VisitConstant(node);
        }
        return result ?? typeof(object);
    }

    //exact match may be in form:
    // sub-expression that use input and output == expression that has a value
    // value-part <BinaryOperator> !inputOrOutput
    // value-part <BinaryOperator> !inputOrOutput
    private void FindExactMatchParts()
    {
        MatchExpressionParts.MatchExpression = _matchExpression;
        var constantTranslationVisitor = new GenericVisitor();
        constantTranslationVisitor.OnVisitBinary(VisitBinary);
        _matchExpression = (LambdaExpression)constantTranslationVisitor.Visit(_matchExpression);

        Expression VisitBinary(BinaryExpression node)
        {
            if (node.Type != typeof(bool))
                return base.VisitBinary(node);

            if (node.NodeType == ExpressionType.Equal)
            {
                if (CanConvertToSimpleString(node.Left) && IsInputOutputExpression(node.Right, out _))
                    _expressionParts.Add(new(node, node.Right, node.Left));
                else if (CanConvertToSimpleString(node.Right) && IsInputOutputExpression(node.Left, out _))
                    _expressionParts.Add(new(node, node.Left, node.Right));
            }

            //translate bool prop like[&& input.IsHappy] to[&& input.IsHappy == true]
            //translate bool prop like[&& !input.IsHappy] to[&& input.IsHappy == false]
            if (IsInputOutputBoolean(node, node.Left, out Expression newExpression1))
                return newExpression1;
            if (IsInputOutputBoolean(node, node.Right, out Expression newExpression2))
                return newExpression2;

            return base.VisitBinary(node);
        }



        bool CanConvertToSimpleString(Expression expression)
        {
            if (expression is ConstantExpression constantExpression)
                return true;

            var useInOut = IsInputOutputExpression(expression, out int inOutUseCount) || inOutUseCount > 0;
            if (useInOut) return false;

            var result = GetExpressionValue(expression);
            if (result != null)
                return result.CanConvertToSimpleString();

            throw new NotSupportedException(
                $"Can't use expression [{expression}] in match because it's evaluated to [NULL].");

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
                    $"Can't use expression [{expression}] in match because we can't compute its value.\n" +
                    $"Try to rewrite it in another form.\n" +
                    $"Exception: {ex}");
            }
        }


    }

    private bool IsInputOutputBoolean(BinaryExpression node, Expression operand, out Expression newExpression)
    {
        newExpression = null;
        var booleanArthimaticOp = new[]
        {
            ExpressionType.And,
            ExpressionType.Or,
            ExpressionType.ExclusiveOr
        };
        var isLeft = node.Left == operand;
        var otherOperand = isLeft ? node.Right : node.Left;
        var isInputOutputBoolean =
            operand.Type == typeof(bool) &&
            IsInputOutputExpression(operand, out _) &&
            (operand is ParameterExpression || operand is MemberExpression) &&
            otherOperand is not ConstantExpression &&
            !booleanArthimaticOp.Contains(node.NodeType);
        var translatedBoolean = isInputOutputBoolean ? MakeBinary(ExpressionType.Equal, operand, Constant(true)) : null;

        if (isInputOutputBoolean is false && operand is UnaryExpression unaryExpression)
        {
            isInputOutputBoolean =
                unaryExpression.NodeType == ExpressionType.Not &&
                IsInputOutputExpression(unaryExpression.Operand, out _) &&
                (unaryExpression.Operand is MemberExpression || unaryExpression.Operand is ParameterExpression);
            translatedBoolean = isInputOutputBoolean ? MakeBinary(ExpressionType.Equal, unaryExpression.Operand, Constant(false)) : null;
        }

        if (isInputOutputBoolean)
        {
            newExpression =
                isLeft ?
                base.VisitBinary(MakeBinary(node.NodeType, translatedBoolean, otherOperand)) :
                base.VisitBinary(MakeBinary(node.NodeType, otherOperand, translatedBoolean));
        }
        return isInputOutputBoolean;
    }


    //change expression part to check to `false`
    //change other simple expressions to `true`
    private void MarkMandatoryParts()
    {
        Expression expressionToCheck = null;

        var changeToBooleans = new GenericVisitor();
        changeToBooleans.OnVisitBinary(VisitBinary);
        changeToBooleans.OnVisitMethodCall(VisitMethodCall);

        //Expression part is mandatory if is fasle and all other parts is true and compiled expression returns false
        foreach (var expressionPart in _expressionParts)
        {
            if (expressionPart.IsMandatory) continue;

            expressionToCheck = expressionPart.Expression;
            var expression = changeToBooleans.Visit(_matchExpression.Body);
            try
            {
                var compiled = Lambda<Func<bool>>(expression).CompileFast();
                expressionPart.IsMandatory = !compiled();
            }
            catch (Exception)
            {
                expressionPart.IsMandatory = false;
            }
        }

        Expression VisitBinary(BinaryExpression node)
        {
            if (node == expressionToCheck)
                return Constant(false);
            if (node.Left == expressionToCheck)
                return base.VisitBinary(MakeBinary(node.NodeType, Constant(false), node.Right));
            if (node.Right == expressionToCheck)
                return base.VisitBinary(MakeBinary(node.NodeType, node.Left, Constant(false)));
            else if (IsSimpleLogicalExpression(node))
                return Constant(true);
            return base.VisitBinary(node);
        }
        Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.ReturnType == typeof(bool))
                return Constant(true);
            return base.VisitMethodCall(methodCallExpression);
        }
    }



    private void SetIsMandatoryPartFullMatchValue()
    {
        var changeToBools = new GenericVisitor();
        changeToBools.OnVisitBinary(VisitBinary);
        changeToBools.OnVisitMethodCall(VisitMethodCall);
        var exp = changeToBools.Visit(_matchExpression.Body);
        var compiled = Lambda<Func<bool>>(exp).CompileFast();
        MatchExpressionParts.IsMandatoryPartFullMatch = compiled();

        //change mandatory parts to true and other to false
        Expression VisitBinary(BinaryExpression node)
        {
            if (_expressionParts.Any(x => x.Expression == node))
                return Constant(true);
            if (IsSimpleLogicalExpression(node))
                return Constant(false);
            return base.VisitBinary(node);
        }

        //if  part is method call that return bool -> make it false
        Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.ReturnType == typeof(bool))
                return Constant(false);
            return base.VisitMethodCall(methodCallExpression);
        }

    }

    private bool IsInputOutputExpression(Expression expression, out int inOutUseCount)
    {
        var checkUseParamter = new GenericVisitor();
        var paramtersUseCount = 0;
        var otherVars = 0;
        //checkUseParamter.StopWhen(_ => paramtersUseCount != -1);
        checkUseParamter.OnVisitParameter(param =>
        {
            if (param == _matchExpression.Parameters[0] || param == _matchExpression.Parameters[1])
                paramtersUseCount++;
            if (param == _matchExpression.Parameters[2] || param == _matchExpression.Parameters[3])
                otherVars++;
            return param;
        });
        checkUseParamter.OnVisitConstant(constant =>
        {
            if (constant.Type.Name.StartsWith(Constants.CompilerClosurePrefix))
                otherVars++;
            return constant;
        });
        checkUseParamter.Visit(expression);
        bool UseInputOutputOnly = paramtersUseCount > 0 && otherVars == 0;
        inOutUseCount = paramtersUseCount;
        return UseInputOutputOnly;
    }
    private bool IsSimpleLogicalExpression(BinaryExpression node)
    {
        var _booleanLogicalOps = new ExpressionType[]
           {
                ExpressionType.AndAlso,
                ExpressionType.OrElse,
                ExpressionType.Equal,
                ExpressionType.NotEqual,
                ExpressionType.LessThan,
                ExpressionType.LessThanOrEqual,
                ExpressionType.GreaterThan,
                ExpressionType.GreaterThanOrEqual,
                ExpressionType.And,
                ExpressionType.Or,
                ExpressionType.ExclusiveOr,
           };
        var logicalOpsCount = 0;
        var countLogicalOpsVisitor = new GenericVisitor();
        countLogicalOpsVisitor.OnVisitBinary(binaryNode =>
        {
            if (_booleanLogicalOps.Contains(binaryNode.NodeType) && binaryNode.Type == typeof(bool))
                logicalOpsCount++;
            return binaryNode;
        });
        countLogicalOpsVisitor.Visit(node);
        return logicalOpsCount == 1 && node.Type == typeof(bool);
    }
}
