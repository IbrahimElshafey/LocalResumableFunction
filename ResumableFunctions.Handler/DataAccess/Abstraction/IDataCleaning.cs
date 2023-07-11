namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IDataCleaning
    {
        Task DeleteCompletedFunctionInstances();
        Task DeactivateUnusedWaitTemplates();
        Task DeleteDeactivatedWaitTemplates();
        Task DeleteSoftDeletedRows();
        Task DeleteOldPushedCalls();
        //todo: Task DeleteUnusedMethodidentifiers();

    }
}
