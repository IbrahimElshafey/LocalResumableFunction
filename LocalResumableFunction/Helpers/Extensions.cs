using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    public static bool SameLambadaSignatures(LambdaExpression? expressionOne, LambdaExpression? expressionTwo)
    {
        var isEqual = expressionOne != null && expressionTwo != null;
        if (isEqual is false) return false;
        if (expressionOne.ReturnType != expressionTwo.ReturnType)
            return false;
        if (expressionOne.Parameters.Count != expressionTwo.Parameters.Count)
            return false;

        for (var i = 0; i < expressionOne.Parameters.Count; i++)
            if (expressionOne.Parameters[i].Type != expressionTwo.Parameters[i].Type)
                return false;
        return true;
    }

    public static bool IsAsyncMethod(MethodBase method)
    {
        var attType = typeof(AsyncStateMachineAttribute);

        // Obtain the custom attribute for the method. 
        // The value returned contains the StateMachineType property. 
        // Null is returned if the attribute isn't present for the method. 
        var attrib = (AsyncStateMachineAttribute)method.GetCustomAttribute(attType);

        return attrib != null;
    }
}