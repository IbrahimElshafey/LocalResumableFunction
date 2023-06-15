using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Helpers.Expressions;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess;

internal class WaitTemplatesRepo : IWaitTemplatesRepo,IDisposable
{
    private readonly FunctionDataContext _context;
    private readonly IServiceScope _scope;

    public WaitTemplatesRepo(IServiceProvider provider)
    {
        _scope = provider.CreateScope();
        _context = _scope.ServiceProvider.GetService<FunctionDataContext>();
    }

    public async Task<WaitTemplate> AddNewTemplate(
        WaitExpressionsHash hashResult,
        object currentFunctionInstance,
        int funcId,
        int groupId,
        int methodId)
    {
        var waitTemplate = new WaitTemplate
        {
            MethodId = methodId,
            FunctionId = funcId,
            MethodGroupId = groupId,
            BaseHash = hashResult.Hash,
        };
        //todo:Rewrite expressions
        var matchWriter = new MatchExpressionWriter(hashResult.MatchExpression, currentFunctionInstance);
        waitTemplate.MatchExpression = matchWriter.MatchExpression;
        waitTemplate.CallMandatoryPartExpression = matchWriter.CallMandatoryPartExpression;
        waitTemplate.InstanceMandatoryPartExpression = matchWriter.InstanceMandatoryPartExpression;
        waitTemplate.IsMandatoryPartFullMatch = matchWriter.IsMandatoryPartFullMatch;


        var setDataWriter = new SetDataExpressionWriter(hashResult.SetDataExpression, currentFunctionInstance.GetType());
        waitTemplate.SetDataExpression = setDataWriter.SetDataExpression;

        _context.WaitTemplates.Add(waitTemplate);
        await _context.SaveChangesAsync();
        return waitTemplate;
    }

    public async Task<WaitTemplate> CheckTemplateExist(byte[] hash, int funcId, int groupId)
    {
        return
             await _context
             .WaitTemplates
             .FirstOrDefaultAsync(x => x.BaseHash == hash && x.MethodGroupId == groupId && x.FunctionId == funcId);
    }

    public async Task<List<WaitTemplate>> GetWaitTemplates(int methodGroupId)
    {
        var result = await
            _context
            .WaitTemplates
            .Select(WaitTemplate.CallMandatoryPartSelector)
            .Where(x => x.MethodGroupId == methodGroupId)
            .AsNoTracking()
            .ToListAsync();
        result.ForEach(x => x.LoadExpressions());
        return result;
    }

    public void Dispose()
    {
        _context?.Dispose();
        _scope?.Dispose();
    }
}
