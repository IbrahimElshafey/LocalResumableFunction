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

    public async Task<WaitTemplate> CheckTemplateExist(byte[] hash, int funcId, int groupId)
    {
        var waitTemplate = (await
            _context.WaitTemplates
            .Select(waitTemplate =>
                    new WaitTemplate
                    {
                        InstanceMandatoryPartExpressionValue = waitTemplate.InstanceMandatoryPartExpressionValue,
                        Id = waitTemplate.Id,
                        FunctionId = waitTemplate.FunctionId,
                        MethodId = waitTemplate.MethodId,
                        MethodGroupId = waitTemplate.MethodGroupId,
                        IsMandatoryPartFullMatch = waitTemplate.IsMandatoryPartFullMatch,
                        ServiceId = waitTemplate.ServiceId,
                        Hash = waitTemplate.Hash,
                        IsActive = waitTemplate.IsActive,
                    })
            .Where(x =>
                x.MethodGroupId == groupId &&
                x.FunctionId == funcId &&
                x.ServiceId == _settings.CurrentServiceId)
            .ToListAsync())
            .FirstOrDefault(x => x.Hash.SequenceEqual(hash));
        if (waitTemplate != null)
        {
            waitTemplate.LoadExpressions();
            if (waitTemplate.IsActive == -1)
            {
                waitTemplate.IsActive = 1;
                await _context.SaveChangesAsync();
            }
        }
        return waitTemplate;
    }


    public async Task<List<WaitTemplate>> GetWaitTemplatesForFunction(int methodGroupId, int functionId)
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
            .Select(waitTemplate =>
                new WaitTemplate
                {
                    MatchExpressionValue = waitTemplate.MatchExpressionValue,
                    SetDataExpressionValue = waitTemplate.SetDataExpressionValue,
                    Id = waitTemplate.Id,
                    FunctionId = waitTemplate.FunctionId,
                    MethodId = waitTemplate.MethodId,
                    MethodGroupId = waitTemplate.MethodGroupId,
                    ServiceId = waitTemplate.ServiceId,
                    IsActive = waitTemplate.IsActive,
                })
            .FirstAsync(x => x.Id == methodWaitTemplateId);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _scope?.Dispose();
    }
}
