namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IDataCleaning
    {
        Task DeleteCompletedFunctionInstances();
        //todo: Task DeleteUnusedWaitTemplates();
        Task DeleteSoftDeletedRows();
        Task DeleteOldPushedCalls();
        //todo: Task DeleteUnusedMethodidentifiers();

    }
}
