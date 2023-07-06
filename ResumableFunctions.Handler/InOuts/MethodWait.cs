using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using ResumableFunctions.Handler.Attributes;
using ResumableFunctions.Handler.Helpers;

namespace ResumableFunctions.Handler.InOuts;
public class MethodWait : Wait
{
    [NotMapped]
    public LambdaExpression SetDataExpression { get; protected set; }

    [NotMapped]
    public LambdaExpression MatchExpression { get; protected set; }

    public string MandatoryPart { get; internal set; }

    [NotMapped]
    internal WaitTemplate Template { get; set; }
    public int TemplateId { get; internal set; }

    [NotMapped]
    internal WaitMethodIdentifier MethodToWait { get; set; }

    internal int? MethodToWaitId { get; set; }

    internal MethodsGroup MethodGroupToWait { get; set; }
    internal int MethodGroupToWaitId { get; set; }

    [NotMapped]
    internal MethodData MethodData { get; set; }

    [NotMapped]
    public object Input { get; set; }

    [NotMapped]
    public object Output { get; set; }


    public bool UpdateFunctionData()
    {
        try
        {
            LoadExpressions();
            if (SetDataExpression == null) return true;
            var setDataExpression = SetDataExpression.CompileFast();
            setDataExpression.DynamicInvoke(Input, Output, CurrentFunction);
            FunctionState.StateObject = CurrentFunction;
            FunctionState.AddLog($"Function instance data updated after wait [{Name}] matched.", LogType.Info, StatusCodes.WaitProcessing);
            return true;
        }
        catch (Exception ex)
        {
            var error = $"An error occurred when try to update function data after method wait [{Name}] matched." + ex.Message;
            FunctionState.AddLog(error, LogType.Error, StatusCodes.WaitProcessing);
            throw new Exception(error, ex);
        }
    }

    public bool IsMatched()
    {
        try
        {
            LoadExpressions();
            if (WasFirst && MatchExpression == null)
                return true;
            if (MethodToWait.MethodInfo ==
                CoreExtensions.GetMethodInfo<LocalRegisteredMethods>(x => x.TimeWait))
                return true;
            var check = MatchExpression.CompileFast();
            return (bool)check.DynamicInvoke(Input, Output, CurrentFunction)!;
        }
        catch (Exception ex)
        {
            var error = $"An error occurred when try evaluate match expression for wait [{Name}]." +
                        ex.Message;
            FunctionState.AddError(error, StatusCodes.WaitProcessing, ex);
            throw new Exception(error, ex);
        }
    }

    internal override bool IsValidWaitRequest()
    {
        if (IsReplay)
            return true;
        switch (WasFirst)
        {
            case false when MatchExpression == null:
                FunctionState.AddError(
                    $"You didn't set the `MatchExpression` for wait [{Name}] that is not a first wait," +
                    $"This will lead to no match for all calls," +
                    $"You can use method MatchIf(Expression<Func<TInput, TOutput, bool>> value) to pass the `MatchExpression`," +
                    $"or use MatchAll() method.", StatusCodes.WaitValidation, null);
                break;
            case true when MatchExpression == null:
                FunctionState.AddLog(
                    $"You didn't set the `MatchExpression` for first wait [{Name}]," +
                    $"This will lead to all calls will be matched.",
                    LogType.Warning, StatusCodes.WaitValidation);
                break;
        }

        if (SetDataExpression == null)
            FunctionState.AddLog(
                $"You didn't set the `SetDataExpression` for wait [{Name}], " +
                $"The execution will not continue, " +
                $"Please use `NoSetData()` if this is intended.", LogType.Warning, StatusCodes.WaitValidation);
        return base.IsValidWaitRequest();
    }

    internal void LoadExpressions()
    {
        if (Template == null) return;
        Template.LoadExpressions();
        MatchExpression = Template.MatchExpression;
        SetDataExpression = Template.SetDataExpression;
        CurrentFunction = (ResumableFunction)FunctionState.StateObject;
    }

    public override void CopyCommonIds(Wait oldWait)
    {
        base.CopyCommonIds(oldWait);
        if (oldWait is MethodWait mw)
        {
            TemplateId = mw.TemplateId;
            MethodToWaitId = mw.MethodToWaitId;
        }

    }
}

public class MethodWait<TInput, TOutput> : MethodWait
{
    public MethodWait(Func<TInput, Task<TOutput>> method) => Initiate(method.Method);
    public MethodWait(Func<TInput, TOutput> method) => Initiate(method.Method);
    public MethodWait(MethodInfo methodInfo) => Initiate(methodInfo);

    private void Initiate(MethodInfo method)
    {
        var methodAttribute =
            method.GetCustomAttribute(typeof(PushCallAttribute));

        if (methodAttribute == null)
            throw new Exception(
                $"You must add attribute `{nameof(PushCallAttribute)}` to method `{method.GetFullName()}`");

        MethodData = new MethodData(method);
        Name = $"#{method.Name}#";
    }

    public MethodWait<TInput, TOutput> SetData(Expression<Func<TInput, TOutput, bool>> value)
    {
        SetDataExpression = value;
        return this;
    }

    public MethodWait<TInput, TOutput> MatchIf(Expression<Func<TInput, TOutput, bool>> value)
    {
        MatchExpression = value;
        return this;
    }

    public MethodWait<TInput, TOutput> MatchAll()
    {
        MatchExpression = (Expression<Func<TInput, TOutput, bool>>)((x, y) => true);
        return this;
    }

    public MethodWait<TInput, TOutput> NoSetData()
    {
        SetDataExpression = (Expression<Func<TInput, TOutput, bool>>)((x, y) => true);
        return this;
    }

}