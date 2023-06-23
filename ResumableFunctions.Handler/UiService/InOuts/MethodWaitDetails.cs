using ResumableFunctions.Handler.InOuts;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Text;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public class MethodWaitDetails
    {
        public MethodWaitDetails(
        string Name,
        WaitStatus Status,
        string FunctionName,
        int InstanceId,
        DateTime Created,
        string MandatoryPart,
        MatchStatus MatchStatus,
        InstanceUpdateStatus InstanceUpdateStatus,
        ExecutionStatus ExecutionStatus,
        TemplateDisplay templateDisplay)
        {
            this.Name = Name;
            this.Status = Status;
            this.FunctionName = FunctionName;
            this.InstanceId = InstanceId;
            this.MatchExpression = templateDisplay.MatchExpression;
            this.SetDataExpression = templateDisplay.SetDataExpression;
            this.Created = Created;
            this.MandatoryPart = MandatoryPart;
            this.MandatoryPartExpression = templateDisplay.MandatoryPartExpression;
            this.MatchStatus = MatchStatus;
            this.InstanceUpdateStatus = InstanceUpdateStatus;
            this.ExecutionStatus = ExecutionStatus;
        }
        public DateTime Created { get; }
        public ExecutionStatus ExecutionStatus { get; }
        public string FunctionName { get; }
        public int InstanceId { get; }
        public InstanceUpdateStatus InstanceUpdateStatus { get; }
        public string MandatoryPart { get; }
        public string MandatoryPartExpression { get; }
        public string MatchExpression { get; }
        public MatchStatus MatchStatus { get; }
        public string Name { get; }
        public string SetDataExpression { get; }
        public WaitStatus Status { get; }
    }
}
