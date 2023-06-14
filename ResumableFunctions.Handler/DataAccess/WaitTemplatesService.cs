using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers.Expressions;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess;

internal class WaitTemplatesService : IWaitTemplatesService
{
    private readonly FunctionDataContext _context;

    public WaitTemplatesService(FunctionDataContext context)
    {
        _context = context;
    }

    public async Task<MethodWaitTemplate> AddNewTemplate(
        WaitExpressionsHash hashResult,
        object currentFunctionInstance,
        int funcId,
        int groupId,
        int methodId)
    {
        var waitTemplate = new MethodWaitTemplate
        {
            MethodId = methodId,
            FunctionId = funcId,
            MethodGroupId = groupId,
            Hash = hashResult.Hash,
        };
        //todo:Rewrite expressions
        var matchWriter = new MatchExpressionWriter(hashResult.MatchExpression, currentFunctionInstance);
        waitTemplate.MatchExpression = matchWriter.MatchExpression;
        waitTemplate.CallMandatoryPartExpression = matchWriter.CallMandatoryPartExpression;
        waitTemplate.CallMandatoryPartExpressionDynamic = matchWriter.CallMandatoryPartExpressionDynamic;
        waitTemplate.InstanceMandatoryPartExpression = matchWriter.InstanceMandatoryPartExpression;
        waitTemplate.IsMandatoryPartFullMatch = matchWriter.IsMandatoryPartFullMatch;
        
        
        var setDataWriter = new SetDataExpressionWriter(hashResult.SetDataExpression, currentFunctionInstance.GetType());
        waitTemplate.SetDataExpression = setDataWriter.SetDataExpression;

        _context.MethodWaitTemplates.Add(waitTemplate);
        await _context.SaveChangesAsync();
        return waitTemplate;
    }

    public async Task<MethodWaitTemplate> CheckTemplateExist(byte[] hash, int funcId, int groupId)
    {
        return
             await _context
             .MethodWaitTemplates
             .FirstOrDefaultAsync(x => x.Hash == hash && x.MethodGroupId == groupId && x.FunctionId == funcId);
    }
}
