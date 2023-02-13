using System.Linq.Expressions;
using LocalResumableFunction.InOuts;
using static System.Linq.Expressions.Expression;

namespace LocalResumableFunction.Helpers;

public class RewriteSetDataExpression : RewriteMatchExpression
{
    private readonly ParameterExpression _functionInstanceArg;
    private readonly MethodWait _wait;

    public RewriteSetDataExpression(MethodWait wait):base(null)
    {
        //  .SetData((input, output) => Result == output);
        //   setDataExpression.DynamicInvoke(pushedMethod.Input, pushedMethod.Output, currentWait.CurrntFunction);
        _wait = wait;
        _functionInstanceArg = Parameter(wait.CurrntFunction.GetType(), "functionInstance");

        var updatedBoy = (LambdaExpression)Visit(wait.SetDataExpression);
        var functionType = typeof(Action<,,>)
            .MakeGenericType(
            updatedBoy.Parameters[0].Type,
            updatedBoy.Parameters[1].Type,
            wait.CurrntFunction.GetType());
        var block = Block(new[] { updatedBoy.Body, Empty() });
        Result = Lambda(
            functionType,
            block,
            updatedBoy.Parameters[0],
            updatedBoy.Parameters[1],
            _functionInstanceArg);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        return base.VisitBinary(node);
    }
    protected override Expression VisitMember(MemberExpression node)
    {
        //replace [FunctionClass].Data.Prop with [_dataParamter.Prop] or constant value
        var x = node.GetDataParamterAccess(_functionInstanceArg);
        if (x.IsFunctionData)
        {
            if (IsBasicType(node.Type))
            {
                object value = GetValue(x.NewExpression);
                if (value != null)
                    return Constant(value, node.Type);
            }

            _wait.NeedFunctionStateForMatch = true;
            return x.NewExpression;
        }

        return base.VisitMember(node);
    }
    //public RewriteSetDataExpression(MethodWait wait)
    //{
    //    var contextProp = wait.SetDataExpression;
    //    if (contextProp.Body is not MemberExpression me)
    //        throw new Exception("When you call `MethodWait.SetProp` the body must be `MemberExpression`");


    //    var functionDataParam = Parameter(wait.CurrntFunction.GetType(), "functionData");
    //    var dataPramterAccess = me.GetDataParamterAccess(functionDataParam);
    //    if (dataPramterAccess.IsFunctionData && dataPramterAccess.NewExpression != null)
    //    {
    //        var methodDataParam = Parameter(wait.GetType(), "MethodData");
    //        var isGenericList = dataPramterAccess.NewExpression.Type.IsGenericType &&
    //                            dataPramterAccess.NewExpression.Type.GetGenericTypeDefinition() == typeof(List<>);
    //        if (isGenericList)
    //        {
    //            var body = Call(dataPramterAccess.NewExpression, dataPramterAccess.NewExpression.Type.GetMethod("Add"),
    //                methodDataParam);
    //            Result = Lambda(body, functionDataParam, methodDataParam);
    //        }
    //        else
    //        {
    //            Expression body = Assign(dataPramterAccess.NewExpression, methodDataParam);
    //            var block = Block(new[] { body, Empty() });
    //            Result = Lambda(block, functionDataParam, methodDataParam);
    //        }
    //    }
    //    else
    //    {
    //        throw new Exception(
    //            "When you call `MethodWait.SetProp` the body must be `MemberExpression` that use the `Data` property.");
    //    }
    //}

}