using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IWaitTemplatesService
    {
        Task<MethodWaitTemplate> AddNewTemplate(
            WaitExpressionsHash hashResult, object currentFunctionInstance, int funcId, int groupId, int methodId);
        Task<MethodWaitTemplate> CheckTemplateExist(byte[] hash, int funcId, int groupId);
    }
}