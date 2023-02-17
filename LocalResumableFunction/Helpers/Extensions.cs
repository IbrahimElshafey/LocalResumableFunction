using System.Linq.Expressions;
using System.Reflection;
using LocalResumableFunction.InOuts;
using static System.Linq.Expressions.Expression;

namespace LocalResumableFunction.Helpers;

public static class Extensions
{
    public static (bool IsFunctionData, MemberExpression? NewExpression) GetDataParamterAccess(
        this MemberExpression node,
        ParameterExpression functionInstanceArg)
    {
        var propAccessStack = new Stack<MemberInfo>();
        var isFunctionData = IsDataAccess(node);
        if (isFunctionData)
        {
            var newAccess = MakeMemberAccess(functionInstanceArg, propAccessStack.Pop());
            while (propAccessStack.Count > 0)
            {
                var currentProp = propAccessStack.Pop();
                newAccess = MakeMemberAccess(newAccess, currentProp);
            }

            return (true, newAccess);
        }

        return (false, null);

        bool IsDataAccess(MemberExpression currentNode)
        {
            propAccessStack.Push(currentNode.Member);
            var subNode = currentNode.Expression;
            if (subNode == null) return false;
            //is function data access 
            var isFunctionDataAccess =
                subNode.NodeType == ExpressionType.Constant && subNode.Type == functionInstanceArg.Type;
            if (isFunctionDataAccess)
                return true;
            if (subNode.NodeType == ExpressionType.MemberAccess)
                return IsDataAccess((MemberExpression)subNode);
            return false;
        }
    }

    public static bool CompareReplaymatchWithOldMatch(LambdaExpression? replayMatch, LambdaExpression? oldMatch)
    {
        var isEqual = replayMatch != null && oldMatch != null;
        if (isEqual is false) return false;
        if (replayMatch.ReturnType != oldMatch.ReturnType) return false;
        for (int i = 0; i < replayMatch.Parameters.Count; i++)
        {
            if (replayMatch.Parameters[i].Type != oldMatch.Parameters[i].Type)
                return false;
        }
        return true;
    }
}