using System.Linq.Expressions;
using ResumableFunctions.Handler.Helpers;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Handler.Expressions;

public class SetDataExpressionWriter : ExpressionVisitor
{
    private readonly ParameterExpression _functionInstanceArg;
    private readonly List<Expression> setValuesExpressions = new();

    public SetDataExpressionWriter(LambdaExpression setDataExpression, Type funcType)
    {
        if (setDataExpression == null)
            return;
        if (setDataExpression?.Parameters.Count == 3)
        {
            return;
        }

        _functionInstanceArg = Parameter(funcType, "functionInstance");

        var updatedBoy = (LambdaExpression)Visit(setDataExpression);
        for (var i = 0; i < setValuesExpressions.Count; i++)
        {
            var setValue = setValuesExpressions[i];
            setValuesExpressions[i] = SwapAssignParts(setValue);
        }

        var functionType = typeof(Action<,,>)
            .MakeGenericType(
                updatedBoy.Parameters[0].Type,
                updatedBoy.Parameters[1].Type,
                funcType);
        setValuesExpressions.Add(Empty());
        var block = Block(setValuesExpressions);
        SetDataExpression = Lambda(
            functionType,
            block,
            updatedBoy.Parameters[0],
            updatedBoy.Parameters[1],
            _functionInstanceArg);
    }

    public LambdaExpression SetDataExpression { get; protected set; }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.Equal)
        {
            var assignVariable = Assign(node.Left, node.Right);
            setValuesExpressions.Add(assignVariable);
        }

        return base.VisitBinary(node);
    }

    protected Expression SwapAssignParts(Expression node)
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