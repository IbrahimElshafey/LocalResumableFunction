using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LocalResumableFunction.InOuts;

public abstract class Wait
{
    private MethodBase _callerMethodInfo;


    //[NotMapped]
    //public MethodBase CallerMethodInfo
    //{
    //    get
    //    {
    //        if (_callerMethodInfo != null)
    //            return _callerMethodInfo;
    //        else return WaitMethodIdentifier?.GetMethodBase();
    //    }

    //    internal set
    //    {
    //        _callerMethodInfo = value;
    //        WaitMethodIdentifier?.SetMethodBase(_callerMethodInfo);
    //    }
    //}

    private ResumableFunctionLocal _currntFunction;

    public int Id { get; internal set; }
    public string Name { get; internal set; }
    public WaitStatus Status { get; internal set; }
    public bool IsFirst { get; internal set; }
    public int StateBeforeWait { get; internal set; }
    public int StateAfterWait { get; internal set; }
    public bool IsNode { get; internal set; }

    public WaitType WaitType { get; internal set; }


    internal ResumableFunctionState FunctionState { get; set; }

    internal int FunctionStateId { get; set; }


    /// <summary>
    ///     The resumable function that initiated requested that wait>
    ///     Will be set by handler.
    /// </summary>
    internal MethodIdentifier RequestedByFunction { get; set; }

    internal int RequestedByFunctionId { get; set; }

    /// <summary>
    ///     If not null this means that wait requested by a sub function
    ///     not
    /// </summary>
    internal Wait ParentWait { get; set; }

    internal List<Wait> ChildWaits { get; set; }

    internal int? ParentWaitId { get; set; }

    [NotMapped]
    internal ResumableFunctionLocal CurrntFunction
    {
        get
        {
            if (FunctionState is not null)
                if (FunctionState.StateObject is JObject stateAsJson)
                {
                    var type = Assembly.LoadFrom(AppContext.BaseDirectory + RequestedByFunction.AssemblyName).GetType(RequestedByFunction.ClassName);
                    var result = stateAsJson.ToObject(type);
                    FunctionState.StateObject = result;
                    _currntFunction = (ResumableFunctionLocal)result;
                    return _currntFunction;
                }
                else if (FunctionState.StateObject is ResumableFunctionLocal result)
                {
                    _currntFunction = result;
                    return _currntFunction;
                }

            return _currntFunction;
        }
        set => _currntFunction = value;
    }
}