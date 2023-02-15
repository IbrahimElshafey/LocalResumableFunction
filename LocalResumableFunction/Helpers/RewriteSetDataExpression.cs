﻿using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using LocalResumableFunction.InOuts;
using static System.Linq.Expressions.Expression;

namespace LocalResumableFunction.Helpers;

public class RewriteSetDataExpression : ExpressionVisitor
{
    private readonly ParameterExpression _functionInstanceArg;
    private readonly MethodWait _wait;
    private List<Expression> setValuesExpressions = new List<Expression>();
    public RewriteSetDataExpression(MethodWait wait)
    {
        //  .SetData((input, output) => Result == output);
        //   setDataExpression.DynamicInvoke(pushedMethod.Input, pushedMethod.Output, currentWait.CurrntFunction);
        _wait = wait;
        _functionInstanceArg = Parameter(wait.CurrntFunction.GetType(), "functionInstance");

        var updatedBoy = (LambdaExpression)Visit(wait.SetDataExpression);
        for (int i = 0; i < setValuesExpressions.Count; i++)
        {
            var setValue = setValuesExpressions[i];
            setValuesExpressions[i] = ChangeDataAccess(setValue);
        }
        var functionType = typeof(Action<,,>)
            .MakeGenericType(
            updatedBoy.Parameters[0].Type,
            updatedBoy.Parameters[1].Type,
            wait.CurrntFunction.GetType());
        setValuesExpressions.Add(Empty());
        var block = Block(setValuesExpressions);
        Result = Lambda(
            functionType,
            block,
            updatedBoy.Parameters[0],
            updatedBoy.Parameters[1],
            _functionInstanceArg);
    }
    public LambdaExpression Result { get; protected set; }
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.Equal)
        {
            var assignVariable = Assign(node.Left, node.Right);
            setValuesExpressions.Add(assignVariable);
        }

        return base.VisitBinary(node);
    }
    protected Expression ChangeDataAccess(Expression node)
    {
        if (node is BinaryExpression binaryExpression)
        {
            if (binaryExpression.Left is MemberExpression dataMemeber)
            {
                var dataAccess = dataMemeber.GetDataParamterAccess(_functionInstanceArg);
                if (dataAccess.IsFunctionData)
                    return Assign(dataAccess.NewExpression, binaryExpression.Right);
            }
            if (binaryExpression.Right is MemberExpression rightDataMemeber)
            {
                var dataAccess = rightDataMemeber.GetDataParamterAccess(_functionInstanceArg);
                if (dataAccess.IsFunctionData)
                    return Assign(dataAccess.NewExpression, binaryExpression.Left);
            }
        }

        return node;
    }
}