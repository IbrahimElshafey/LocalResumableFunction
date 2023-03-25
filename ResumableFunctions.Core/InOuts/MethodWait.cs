using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.Helpers;

namespace ResumableFunctions.Core.InOuts;

public class MethodWait : Wait
{

    [NotMapped] public LambdaExpression SetDataExpression { get; internal set; }

    internal int PushedMethodCallId { get; set; }
    internal byte[] SetDataExpressionValue { get; set; }

    [NotMapped]
    public LambdaExpression MatchIfExpression { get; internal set; }

    internal byte[] MatchIfExpressionValue { get; set; }

    public bool NeedFunctionStateForMatch { get; internal set; } = false;


    /// <summary>
    ///     The method that we wait to resume resumable function
    /// </summary>
    internal MethodIdentifier WaitMethodIdentifier { get; set; }

    [NotMapped]
    internal MethodData MethodData { get; set; }

    internal int WaitMethodIdentifierId { get; set; }

    [NotMapped]
    public object Input { get; set; }

    [NotMapped]
    public object Output { get; set; }

    private Assembly FunctionAssembly =>
        RequestedByFunction?.MethodInfo.DeclaringType.Assembly ??
        WaitMethodIdentifier?.MethodInfo.DeclaringType.Assembly;

    internal void RewriteExpressions()
    {
        //Rewrite Match Expression
        MatchIfExpression = new RewriteMatchExpression(this).Result;
        MatchIfExpressionValue =
            TextCompressor.CompressString(ExpressionToJsonConverter.ExpressionToJson(MatchIfExpression, FunctionAssembly));
        
        //Rewrite SetData Expression
        SetDataExpression = new RewriteSetDataExpression(this).Result;
        SetDataExpressionValue =
            TextCompressor.CompressString(ExpressionToJsonConverter.ExpressionToJson(SetDataExpression, FunctionAssembly));
    }



    internal void LoadExpressions()
    {
        MatchIfExpression = (LambdaExpression)
            ExpressionToJsonConverter.JsonToExpression(
                TextCompressor.DecompressString(MatchIfExpressionValue), FunctionAssembly);
        SetDataExpression = (LambdaExpression)
            ExpressionToJsonConverter.JsonToExpression(
                TextCompressor.DecompressString(SetDataExpressionValue), FunctionAssembly);
    }

    public void UpdateFunctionData()
    {
        try
        {
            var setDataExpression = SetDataExpression.Compile();
            setDataExpression.DynamicInvoke(Input, Output, CurrentFunction);
            FunctionState.StateObject = CurrentFunction;
            if(IsFirst is false)
                FunctionState.Status = FunctionStatus.InProgress;
        }
        catch (Exception)
        {
            FunctionState.Status = FunctionStatus.ErrorOccured;
            FunctionState.StatusMessage += $"An error occured when try to update function data after method wait [{Name}] matched.";
            throw;
        }
    }

    public bool IsMatched()
    {
        try
        {
            if (IsFirst && MatchIfExpressionValue == null)
                return true;
            var check = MatchIfExpression.Compile();
            return (bool)check.DynamicInvoke(Input, Output, CurrentFunction);
        }
        catch (Exception e)
        {
            return false;
        }
    }
}

public class MethodWait<TInput, TOutput> : MethodWait
{
    public MethodWait(Func<TInput, Task<TOutput>> method)
    {
        Initiate(method.Method);
    }
    public MethodWait(Func<TInput, TOutput> method)
    {
        Initiate(method.Method);
    }

    private void Initiate(MethodInfo method)
    {
        var methodAttribute =
            method.GetCustomAttribute(typeof(WaitMethodAttribute)) ??
            method.GetCustomAttribute(typeof(WaitMethodImplementationAttribute)) ??
            method.GetCustomAttribute(typeof(ExternalWaitMethodAttribute));
        if (methodAttribute == null)
            throw new Exception(
                $"You must add attribute [WaitMethod , WaitMethodImplementation or ExternalWaitMethod] to method {method.GetFullName()}");

        switch (methodAttribute)
        {
            case WaitMethodAttribute:
                MethodData = new MethodData(method);
                break;
            case WaitMethodImplementationAttribute:
                MethodInfo interfaceMethod = method.GetInterfaceMethod();
                if (interfaceMethod == null)
                    throw new Exception(
                        $"No interface method matched for method [{method.GetFullName()}]");
                var waitMethodAttributeExist = interfaceMethod.GetCustomAttribute(typeof(WaitMethodAttribute));
                if (waitMethodAttributeExist == null)
                    throw new Exception(
                        $"You must add attribute [WaitMethodAttribute] to interface method {method.GetFullName()}");
                MethodData = new MethodData(interfaceMethod);
                break;
            case ExternalWaitMethodAttribute externalWaitMethodAttribute:
                MethodData = new MethodData(method, externalWaitMethodAttribute);
                break;
        }

        Name = $"#{method.Name}#";
    }

    public MethodWait<TInput, TOutput> SetData(Expression<Func<TInput, TOutput, bool>> value)
    {
        SetDataExpression = value;
        return this;
    }

    public MethodWait<TInput, TOutput> MatchIf(Expression<Func<TInput, TOutput, bool>> value)
    {
        MatchIfExpression = value;
        return this;
    }

}