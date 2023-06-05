using ResumableFunctions.Handler.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace ResumableFunctions.Handler.InOuts;

public class MethodWaitTemplate : IEntity
{
  
    public int Id { get; internal set; }
    public int FunctionId { get; internal set; }
    public int MethodGroupId { get; internal set; }
    public MethodsGroup MethodGroup { get; internal set; }
    public byte[] HashId { get; internal set; }
    public DateTime Created { get; internal set; }

    public LambdaExpression MatchExpression { get; internal set; }
    public Expression<Func<ExpandoAccessor, ExpandoAccessor, bool>> MatchExpressionDynamic { get; internal set; }
    public LambdaExpression SetDataExpression { get; internal set; }
    public Expression<Action<ExpandoAccessor, ExpandoAccessor>> SetDataExpressionDynamic { get; internal set; }
    public Expression<Func<ExpandoAccessor, string[]>> MandatoryPartExtractorExpression { get; internal set; }
    public Expression<Func<ExpandoAccessor,string>> WaitMandatoryPartExpression { get; internal set; }

}
