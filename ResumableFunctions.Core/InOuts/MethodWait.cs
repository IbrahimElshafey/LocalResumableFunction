using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ResumableFunctions.Core.Attributes;
using ResumableFunctions.Core.Helpers;

namespace ResumableFunctions.Core.InOuts;

public class MethodWait : Wait
{
    public int PushedMethodCallId { get; internal set; }
    [NotMapped] public LambdaExpression SetDataExpression { get; internal set; }

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

    //todo:bug
    private Assembly FunctionAssembly =>
        WaitMethodIdentifier?.MethodInfo?.DeclaringType.Assembly ??
        RequestedByFunction?.MethodInfo?.DeclaringType.Assembly ??
        Assembly.GetEntryAssembly();

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

    public bool UpdateFunctionData()
    {
        try
        {
            var setDataExpression = SetDataExpression.Compile();
            setDataExpression.DynamicInvoke(Input, Output, CurrentFunction);
            FunctionState.StateObject = CurrentFunction;
            FunctionState.LogStatus(
                FunctionStatus.Progress,
                $"Method wait [{Name}] matched and function data updated.");
            return true;
        }
        catch (Exception ex)
        {
            FunctionState.LogStatus(
                FunctionStatus.Error,
                $"An error occured when try to update function data after method wait [{Name}] matched." +
                ex.Message);
            return false;
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
            FunctionState.LogStatus(
               FunctionStatus.Error,
               $"An error occured when try evaluate match for wait [{Name}]." +
               e.Message);
            return false;
        }
    }

    internal override void Cancel()
    {
        base.Cancel();
        if (Name == $"#{nameof(LocalRegisteredMethods.TimeWait)}#" && ExtraData is JObject waitDataJson)
        {
            var waitData = waitDataJson.ToObject<TimeWaitData>();
            var client = CoreExtensions.GetServiceProvider().GetService<IBackgroundJobClient>();
            client.Delete(waitData.JobId);
        }
    }

    internal override (bool Valid, string Message) ValidateWaitRequest()
    {
        if (!IsFirst && MatchIfExpression == null)
            FunctionState.LogStatus(
                FunctionStatus.Error,
                $"You didn't set the `MatchIfExpression` for wait [{Name}] that is not a first wait," +
                $"This will lead to no match for all calls," +
                $"You can use method MatchIf(Expression<Func<TInput, TOutput, bool>> value) to pass the `MatchIfExpression`," +
                $"or use MatchAll() method.");
        if (IsFirst && MatchIfExpression == null)
            FunctionState.LogStatus(
                FunctionStatus.Warning,
                $"You didn't set the `MatchIfExpression` for first wait [{Name}]," +
                $"This will lead to all calls will be matched.");
        if (SetDataExpression == null)
            FunctionState.LogStatus(
                FunctionStatus.Error,
                $"You didn't set the `SetDataExpression` for wait [{Name}], " +
                $"The execution will not continue, " +
                $"Please use `NoSetData()` if this is intended.");
        return base.ValidateWaitRequest();
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

    public MethodWait<TInput, TOutput> MatchAll()
    {
        MatchIfExpression = (Expression<Func<TInput, TOutput, bool>>)((x, y) => true);
        return this;
    }

    public MethodWait<TInput, TOutput> NoSetData()
    {
        SetDataExpression = (Expression<Func<TInput, TOutput, bool>>)((x, y) => true);
        return this;
    }

    internal override (bool Valid, string Message) ValidateWaitRequest()
    {
        //if (!IsFirst && MatchIfExpression == null)
        //    FunctionState.LogStatus(
        //        FunctionStatus.Error,
        //        $"You didn't set the `MatchIfExpression` for wait [{Name}] that is not a first wait," +
        //        $"This will lead to no match for all calls.");
        //Todo:validate type serialization
        return base.ValidateWaitRequest();
    }

}