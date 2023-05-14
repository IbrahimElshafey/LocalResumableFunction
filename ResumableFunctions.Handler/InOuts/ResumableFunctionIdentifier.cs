using System.Net.NetworkInformation;

namespace ResumableFunctions.Handler.InOuts;

public class ResumableFunctionIdentifier : MethodIdentifier
{
    public string RF_MethodUrn { get; internal set; }
    public List<Wait> WaitsCreatedByFunction { get; internal set; }
    public List<ResumableFunctionState> ActiveFunctionsStates { get; internal set; }

    internal override void FillFromMethodData(MethodData methodData)
    {
        RF_MethodUrn = methodData.MethodUrn;
        base.FillFromMethodData(methodData);
    }
}

