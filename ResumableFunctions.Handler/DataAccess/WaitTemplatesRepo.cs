using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Handler.DataAccess.Abstraction;
using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess;

internal class WaitTemplatesRepo : IWaitTemplatesRepo, IDisposable
{
    private readonly WaitsDataContext _context;
    private readonly IServiceScope _scope;
    private readonly IResumableFunctionsSettings _settings;

    public WaitTemplatesRepo(IServiceProvider provider, IResumableFunctionsSettings settings)
    {
        _settings = settings;
        _scope = provider.CreateScope();
        _context = _scope.ServiceProvider.GetService<WaitsDataContext>();
    }

    public async Task<WaitTemplate> AddNewTemplate(
        ExpressionsHashCalculator hashResult,
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
            Hash = hashResult.Hash,
        };

        var matchWriter = new MatchExpressionWriter(hashResult.MatchExpression, currentFunctionInstance);
        waitTemplate.MatchExpression = matchWriter.MatchExpression;
        waitTemplate.CallMandatoryPartExpression = matchWriter.CallMandatoryPartExpression;
        waitTemplate.InstanceMandatoryPartExpression = matchWriter.InstanceMandatoryPartExpression;
        waitTemplate.IsMandatoryPartFullMatch = matchWriter.IsMandatoryPartFullMatch;


        var setDataWriter = new SetDataExpressionWriter(hashResult.SetDataExpression, currentFunctionInstance.GetType());
        waitTemplate.SetDataExpression = setDataWriter.SetDataExpression;

        _context.WaitTemplates.Add(waitTemplate);
        await DeleteUnusedTemplateSiblings(waitTemplate);
        await _context.SaveChangesAsync();
        return waitTemplate;
    }

    private async Task DeleteUnusedTemplateSiblings(WaitTemplate waitTemplate)
    {
        var templateSiblings =
            await _context.WaitTemplates
            .Where(template =>
                template.MethodGroupId == waitTemplate.MethodGroupId &&
                template.MethodId == waitTemplate.MethodId &&
                template.FunctionId == waitTemplate.FunctionId
            )
            .Select(x => x.Id)
            .ToListAsync();
        if (templateSiblings?.Any() is true)
        {
            var templatesToDelete =
                templateSiblings.Except(
                   await _context.MethodWaits
                   .Where(mw =>
                        mw.Status == WaitStatus.Waiting &&
                        templateSiblings.Contains(mw.TemplateId))
                   .Select(x => x.TemplateId)
                   .Distinct()
                   .ToListAsync());
            await _context.WaitTemplates
                .Where(template => templatesToDelete.Contains(template.Id))
                .ExecuteDeleteAsync();
        }
    }

    public async Task<WaitTemplate> CheckTemplateExist(byte[] hash, int funcId, int groupId)
    {
        var result = (await _context
            .WaitTemplates
            .Select(WaitTemplate.InstanceMandatoryPartSelector)
            .Where(x =>
                x.MethodGroupId == groupId &&
                x.FunctionId == funcId &&
                x.ServiceId == _settings.CurrentServiceId)
            .ToListAsync())
            .FirstOrDefault(x => x.Hash.SequenceEqual(hash));
        result?.LoadExpressions();
        return result;
    }


    public async Task<List<WaitTemplate>> GetWaitTemplates(int methodGroupId, int functionId)
    {
        var waitTemplatesQry = _context
            .WaitTemplates
            .Where(template =>
                template.FunctionId == functionId &&
                template.MethodGroupId == methodGroupId &&
                template.ServiceId == _settings.CurrentServiceId);

        var result = await
            waitTemplatesQry
            .AsNoTracking()
            .ToListAsync();

        result.ForEach(x => x.LoadExpressions());
        return result;
    }
    public async Task<List<WaitTemplate>> GetWaitTemplates(int methodGroupId)
    {
        var templateIds = await _context
            .MethodWaits
            .Where(x =>
                x.Status == WaitStatus.Waiting &&
                x.MethodGroupToWaitId == methodGroupId &&
                x.ServiceId == _settings.CurrentServiceId)
            .Select(x => x.TemplateId)
            .Distinct()
            .ToListAsync();

        var waitTemplatesQry = _context
            .WaitTemplates
            .Select(WaitTemplate.CallMandatoryPartSelector)
            .Where(x => templateIds.Contains(x.Id));

        var result = await
            waitTemplatesQry
            .AsNoTracking()
            .ToListAsync();

        result.ForEach(x => x.LoadExpressions());
        return result;
    }

    public async Task<WaitTemplate> GetById(int templateId)
    {
        var waitTemplate = await _context.WaitTemplates.FindAsync(templateId);
        waitTemplate?.LoadExpressions();
        return waitTemplate;
    }

    public async Task<WaitTemplate> GetWaitTemplateWithBasicMatch(int methodWaitTemplateId)
    {
        return
            await _context
            .WaitTemplates
            .Select(WaitTemplate.BasicMatchSelector)
            .FirstAsync(x => x.Id == methodWaitTemplateId);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _scope?.Dispose();
    }
}
