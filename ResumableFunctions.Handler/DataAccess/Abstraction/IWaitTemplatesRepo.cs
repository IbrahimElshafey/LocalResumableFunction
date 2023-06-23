using ResumableFunctions.Handler.Expressions;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IWaitTemplatesRepo
    {
        Task<WaitTemplate> AddNewTemplate(
            WaitExpressionsHash hashResult, object currentFunctionInstance, int funcId, int groupId, int methodId);
        Task<WaitTemplate> CheckTemplateExist(byte[] hash, int funcId, int groupId);
        Task<List<WaitTemplate>> GetWaitTemplates(int methodGroupId);
    }
}