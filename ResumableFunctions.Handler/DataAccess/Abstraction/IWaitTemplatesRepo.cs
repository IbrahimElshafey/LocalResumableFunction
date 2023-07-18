using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IWaitTemplatesRepo
    {
        Task<WaitTemplate> AddNewTemplate(ExpressionsHashCalculator hashResult, object currentFunctionInstance,
            int funcId, int groupId, int methodId, int inCodeLine);
        Task<WaitTemplate> CheckTemplateExist(byte[] hash, int funcId, int groupId);
        Task<List<WaitTemplate>> GetWaitTemplatesForFunction(int methodGroupId, int functionId);
        Task<WaitTemplate> GetById(int templateId);
        Task<WaitTemplate> GetWaitTemplateWithBasicMatch(int methodWaitTemplateId);
    }
}