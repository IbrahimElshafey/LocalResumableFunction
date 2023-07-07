using ResumableFunctions.Handler.InOuts;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Text;

namespace ResumableFunctions.Handler.UiService.InOuts
{
    public class MethodWaitDetails
    {
        public MethodWaitDetails(string name,
            int id,
            WaitStatus status,
            string functionName,
            int instanceId,
            DateTime created,
            string mandatoryPart,
            MatchStatus matchStatus,
            InstanceUpdateStatus instanceUpdateStatus,
            ExecutionStatus executionStatus,
            TemplateDisplay templateDisplay)
        {
            Name = name;
            Id = id;
            Status = status;
            FunctionName = functionName;
            InstanceId = instanceId;
            MatchExpression = templateDisplay.MatchExpression;
            SetDataExpression = templateDisplay.SetDataExpression;
            Created = created;
            MandatoryPart = mandatoryPart;
            MandatoryPartExpression = templateDisplay.MandatoryPartExpression;
            MatchStatus = matchStatus;
            InstanceUpdateStatus = instanceUpdateStatus;
            ExecutionStatus = executionStatus;
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
        public int Id { get; }
        public string SetDataExpression { get; }
        public WaitStatus Status { get; }
        public int? CallId { get; set; }
        public string GroupName { get; set; }
    }
}
