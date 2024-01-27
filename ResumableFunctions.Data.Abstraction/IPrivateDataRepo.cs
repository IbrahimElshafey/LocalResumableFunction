using ResumableFunctions.Data.Abstraction.Entities;

namespace ResumableFunctions.Data.Abstraction
{
    public interface IPrivateDataRepo
    {
        Task<PrivateData> GetPrivateData(long id);
    }
}