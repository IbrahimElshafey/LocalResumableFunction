using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers;
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
        var toAdd = new MethodWaitTemplate
        {
            MethodId = methodId,
            FunctionId = funcId,
            MethodGroupId = groupId,
            Hash = hashResult.Hash,
        };
        //todo:Rewrite expressions
        var matchWriter = new MatchExpressionWriter(hashResult.MatchExpression, currentFunctionInstance);
        toAdd.MatchExpression = matchWriter.MatchExpression;
        
        var setDataWriter = new SetDataExpressionWriter(hashResult.SetDataExpression, currentFunctionInstance.GetType());
        toAdd.SetDataExpression = setDataWriter.SetDataExpression;

        _context.MethodWaitTemplates.Add(toAdd);
        await _context.SaveChangesAsync();
        return toAdd;
    }

    public Task<MethodWaitTemplate> CheckTemplateExist(byte[] hash, int funcId, int groupId)
    {
        return
             _context
             .MethodWaitTemplates
             .FirstOrDefaultAsync(x => x.Hash == hash && x.MethodGroupId == groupId && x.FunctionId == funcId);
    }
}
