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

    public async Task<WaitTemplate> AddNewTemplate(ExpressionsHashCalculator hashResult,
        object currentFunctionInstance,
        int funcId,
        int groupId,
        int methodId,
        int inCodeLine)
    {
        var waitTemplate = new WaitTemplate
        {
            MethodId = methodId,
            FunctionId = funcId,
            MethodGroupId = groupId,
            Hash = hashResult.Hash,
            InCodeLine = inCodeLine,
            IsActive = 1,
        };

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

    public async Task DeactivateUnusedTemplateSiblings(WaitTemplate waitTemplate)
    {
        //todo:problem for waits in same group?
        var templateSiblings =
            await _context.WaitTemplates
            .Where(template =>
                template.MethodGroupId == waitTemplate.MethodGroupId &&
                template.MethodId == waitTemplate.MethodId &&
                template.FunctionId == waitTemplate.FunctionId &&
                template.InCodeLine == waitTemplate.InCodeLine
            )
            .Select(x => x.Id)
            .ToListAsync();
        if (templateSiblings.Any())
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
                .ExecuteUpdateAsync(x => x.SetProperty(x => x.IsActive, -1));
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
                x.ServiceId == _settings.CurrentServiceId &&
                x.IsActive == 1)
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
                template.ServiceId == _settings.CurrentServiceId &&
                template.IsActive == 1);

        var result = await
            waitTemplatesQry
            .OrderByDescending(x => x.Id)
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
