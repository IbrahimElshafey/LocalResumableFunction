using ResumableFunctions.Handler.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.Helpers
{
    internal static class MethodInvoker
    {

        public static bool CallGroupMatchFunc(object instance, string methodName, WaitsGroup input)
        {
            var methodInfo = GetMethodInfo(ref instance, methodName);
            return (bool)methodInfo.Invoke(instance, new object[] { input });
        }

        public static void CallCancelAction(object instance, string methodName)
        {
            var methodInfo = GetMethodInfo(ref instance, methodName);
            methodInfo.Invoke(instance, (object[])null);
        }

        public static void CallAfterMatchAction<TInput, TOutput>(object instance, string methodName, TInput input, TOutput output)
        {
            var methodInfo = GetMethodInfo(ref instance, methodName);
            methodInfo.Invoke(instance, new object[] { input, output });
        }

        private static MethodInfo GetMethodInfo(ref object instance, string methodName)
        {
            var classType = instance.GetType();
            var methodInfo = classType.GetMethod(methodName, Flags());

            if (methodInfo == null)
            {
                var lambdasClass = classType.GetNestedType("<>c", BindingFlags.NonPublic);
                methodInfo = lambdasClass.GetMethod(methodName, Flags());
                instance = Activator.CreateInstance(lambdasClass);
            }

            if (methodInfo == null)
                throw new NullReferenceException(
                    $"Can't find method [{methodName}] in class [{classType.Name}]");
            return methodInfo;
        }

        private static BindingFlags Flags() =>
            BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    }
}
