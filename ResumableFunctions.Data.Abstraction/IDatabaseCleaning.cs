using System.Threading.Tasks;

namespace ResumableFunctions.Data.Abstraction
{
    public interface IDatabaseCleaning
    {
        Task CleanCompletedFunctionInstances();
        Task MarkInactiveWaitTemplates();
        Task CleanInactiveWaitTemplates();
        Task CleanSoftDeletedRows();
        Task CleanOldPushedCalls();

        //todo: Task DeleteInactiveMethodidentifiers();
    }
}
