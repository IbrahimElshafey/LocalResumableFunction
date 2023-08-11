using FastExpressionCompiler;
using System.Linq.Expressions;

namespace ResumableFunctions.Handler.Expressions;

public class MatchExpressionParts
{
    public LambdaExpression MatchExpression { get; internal set; }
    public LambdaExpression CallMandatoryPartExpression { get; internal set; }
    public LambdaExpression InstanceMandatoryPartExpression { get; internal set; }
    public bool IsMandatoryPartFullMatch { get; internal set; }
    public object Closure { get; internal set; }
    public string GetInstanceMandatoryPart(object currentFunction)
    {
        if (InstanceMandatoryPartExpression != null)
        {
            var partIdFunc = InstanceMandatoryPartExpression.CompileFast();
            var parts = (object[])partIdFunc.DynamicInvoke(currentFunction, Closure);
            if (parts?.Any() == true)
                return string.Join("#", parts);
        }
        return null;
    }

    public string GetPushedCallMandatoryPart(object input, object output)
    {
        var getMandatoryFunc = CallMandatoryPartExpression.CompileFast();
        var parts = (object[])getMandatoryFunc.DynamicInvoke(input, output);
        return string.Join("#", parts);
    }
}
