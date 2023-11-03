using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.DataAccess.Abstraction
{
    public interface IPrivateDataRepo
    {
        Task<PrivateData> GetPrivateData(Guid guid);
    }
}