using LocalResumableFunction.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LocalResumableFunction.InOuts
{
    public class MethodWait : Wait
    {
        public ManyMethodsWait ParentWaitsGroup { get; internal set; }

        public int? ParentWaitsGroupId { get; internal set; }

        public bool IsOptional { get; internal set; } = false;

        public LambdaExpression SetDataExpression { get; internal set; }
        public LambdaExpression MatchIfExpression { get; internal set; }
        public bool NeedFunctionStateForMatch { get; internal set; } = false;

    }
    public class MethodWait<Input, Output> : MethodWait
    {
        public MethodWait(Func<Input, Output> method)
        {
            var eventMethodAttributeExist = method.Method.GetCustomAttribute(typeof(WaitMethodAttribute));
            if (eventMethodAttributeExist == null)
                throw new Exception($"You must add attribute [{nameof(WaitMethodAttribute)}] to method {method.Method.Name}");
            WaitMethodIdentifier = ResumableFunctionHandler.GetMethodIdentifier(method.Method);
        }
        public MethodWait<Input, Output> SetData(Expression<Action<Input, Output>> value)
        {
            SetDataExpression = value;
            //todo:rewrite set data expression
            return this;
        }

        public MethodWait<Input, Output> If(Expression<Func<Input, Output, bool>> value)
        {
            MatchIfExpression = value;
            //todo:rewrite MatchIfExpression
            return this;
        }

        public MethodWait<Input, Output> SetOptional()
        {
            IsOptional = true;
            return this;
        }
    }
}