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

    //todo:bug
    private Assembly FunctionAssembly =>
        MethodToWait?.MethodInfo?.DeclaringType.Assembly ??
        RequestedByFunction?.MethodInfo?.DeclaringType.Assembly ??
        Assembly.GetEntryAssembly();

    public bool UpdateFunctionData()
    {
        try
        {
            LoadExpressions();
            var setDataExpression = SetDataExpression.CompileFast();
            setDataExpression.DynamicInvoke(Input, Output, CurrentFunction);
            FunctionState.StateObject = CurrentFunction;
            FunctionState.AddLog(
                $"Function instance data updated after wait [{Name}] matched.",
                LogType.Info);//todo:function state in progress
            return true;
        }
        catch (Exception ex)
        {
            FunctionState.AddLog(
                $"An error occured when try to update function data after method wait [{Name}] matched." +
                ex.Message,
                LogType.Error);
            return false;
        }
    }

    public bool IsMatched()
    {
        try
        {
            LoadExpressions();
            if (IsFirst && MatchExpression == null)
                return true;
            var check = MatchExpression.CompileFast();
            return (bool)check.DynamicInvoke(Input, Output, CurrentFunction);
        }
        catch (Exception ex)
        {
            FunctionState.AddError(
               $"An error occured when try evaluate match expression for wait [{Name}]." +
               ex.Message,
               ex);
            return false;
        }
    }

    internal override bool IsValidWaitRequest()
    {
        //Todo:validate type serialization
        if (!IsFirst && MatchExpression == null)
            FunctionState.AddError(
                $"You didn't set the `MatchIfExpression` for wait [{Name}] that is not a first wait," +
                $"This will lead to no match for all calls," +
                $"You can use method MatchIf(Expression<Func<TInput, TOutput, bool>> value) to pass the `MatchIfExpression`," +
                $"or use MatchAll() method.");
        if (IsFirst && MatchExpression == null)
            FunctionState.AddLog(
                $"You didn't set the `MatchIfExpression` for first wait [{Name}]," +
                $"This will lead to all calls will be matched.",
                LogType.Warning);
        if (SetDataExpression == null)
            FunctionState.AddError(
                $"You didn't set the `SetDataExpression` for wait [{Name}], " +
                $"The execution will not continue, " +
                $"Please use `NoSetData()` if this is intended.");
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
                $"You must add attribute [WaitMethod , WaitMethodImplementation or ExternalWaitMethod] to method {method.GetFullName()}");

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