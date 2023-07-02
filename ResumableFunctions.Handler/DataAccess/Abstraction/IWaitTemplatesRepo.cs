using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IWaitTemplatesRepo
    {
        Task<WaitTemplate> AddNewTemplate(
            WaitExpressionsHash hashResult, object currentFunctionInstance, long funcId, long groupId, long methodId);
        Task<WaitTemplate> CheckTemplateExist(byte[] hash, long funcId, long groupId);
        Task<List<WaitTemplate>> GetWaitTemplates(long methodGroupId);
        Task<WaitTemplate> GetById(long templateId);
    }
}