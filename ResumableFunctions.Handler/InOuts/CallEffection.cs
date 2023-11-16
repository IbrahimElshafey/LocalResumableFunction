namespace ResumableFunctions.Handler.InOuts;
public class CallEffection
{

    public int AffectedServiceId { get; set; }
    public string AffectedServiceName { get; set; }
    public string AffectedServiceUrl { get; set; }

    public long CallId { get; set; }
    public string MethodUrn { get; set; }
    public int MethodGroupId { get; set; }

    public List<int> AffectedFunctionsIds { get; set; }

    public override string ToString()
    {
        return $"Put pushed call [{MethodUrn}:{CallId}] in the processing queue for service [{AffectedServiceName}].";
    }
}
