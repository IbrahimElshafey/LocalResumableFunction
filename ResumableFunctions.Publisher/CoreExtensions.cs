using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using static System.Linq.Expressions.Expression;

namespace ResumableFunctions.Publisher
{
    public static class CoreExtensions
    {
        private static IServiceProvider _ServiceProvider;
        public static IServiceProvider GetServiceProvider() => _ServiceProvider;
        public static void SetServiceProvider(IServiceProvider provider) => _ServiceProvider = provider;


        public static (bool IsFunctionData, MemberExpression NewExpression) GetDataParamterAccess(
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

        public static bool SameMatchSignature(LambdaExpression expressionOne, LambdaExpression expressionTwo)
        {
            var isEqual = expressionOne != null && expressionTwo != null;
            if (isEqual is false) return false;
            if (expressionOne.ReturnType != expressionTwo.ReturnType)
                return false;
            if (expressionOne.Parameters.Count != expressionTwo.Parameters.Count)
                return false;
            var minParamsCount = Math.Min(expressionOne.Parameters.Count, expressionTwo.Parameters.Count);
            for (var i = 0; i < minParamsCount; i++)
                if (expressionOne.Parameters[i].Type != expressionTwo.Parameters[i].Type)
                    return false;
            return true;
        }

        public static bool IsAsyncMethod(this MethodBase method)
        {
            var attType = typeof(AsyncStateMachineAttribute);

            // Obtain the custom attribute for the method. 
            // The value returned contains the StateMachineType property. 
            // Null is returned if the attribute isn't present for the method. 
            var attrib = (AsyncStateMachineAttribute)method.GetCustomAttribute(attType);


            if (attrib == null)
            {
                bool returnTypeIsTask =
                  attrib == null &&
                  method is MethodInfo mi &&
                  mi != null &&
                  mi.ReturnType.IsGenericType &&
                  mi.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
                return returnTypeIsTask;
            }
            return true;
        }


        public static string GetFullName(this MethodBase method)
        {
            return $"{method.DeclaringType.FullName}.{method.Name}";
        }
        public static MethodInfo GetInterfaceMethod(this MethodInfo method)
        {
            var type = method.DeclaringType;
            foreach (Type interf in type.GetInterfaces())
            {
                foreach (MethodInfo interfaceMethod in interf.GetMethods())
                {
                    bool sameSiganture =
                        interfaceMethod.Name == method.Name &&
                        interfaceMethod.ReturnType == method.ReturnType &&
                        interfaceMethod.GetParameters().Select(x => x.ParameterType).SequenceEqual(method.GetParameters().Select(x => x.ParameterType));
                    if (sameSiganture)
                        return interfaceMethod;
                }
            }
            return null;
        }

        //public static MethodInfo GetMethodInfo(string AssemblyName, string ClassName, string MethodName,string MethodSignature)
        //{
        //    MethodInfo _methodInfo = null;
        //    string assemblyPath = $"{AppContext.BaseDirectory}{AssemblyName}.dll";
        //    if (File.Exists(assemblyPath))
        //        if (AssemblyName != null && ClassName != null && MethodName != null)
        //        {
        //            _methodInfo = Assembly.LoadFrom(assemblyPath)
        //                .GetType(ClassName)
        //                ?.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        //                .FirstOrDefault(x => x.Name == MethodName && MethodData.CalcSignature(x) == MethodSignature);
        //            return _methodInfo;
        //        }

        //    return _methodInfo;
        //}
    }
}