using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.Data;
using ResumableFunctions.Handler.UiService.InOuts;

namespace ResumableFunctions.Handler.UiService
{
    public class UiService : IUiService
    {
        private readonly FunctionDataContext _context;

        public UiService(FunctionDataContext context)
        {
            _context = context;
        }

        public async Task<MainStatistics> GetMainStatistics()
        {
            var services =
                await _context.ServicesData.CountAsync(x => x.ParentId == -1);
            var resumableFunction =
                await _context.ResumableFunctionIdentifiers.CountAsync(x => x.Type == Handler.InOuts.MethodType.ResumableFunctionEntryPoint);
            var resumableFunctionsInstances = await _context.FunctionStates.CountAsync();
            var methods = await _context.WaitMethodIdentifiers.CountAsync();
            var pushedCalls = await _context.PushedCalls.CountAsync();
            var latestLogErrors =
                await
                _context
                .Logs
                .OrderByDescending(x => x.Id)
                .Take(20)
                .CountAsync(x => x.Type == Handler.InOuts.LogType.Error);
            return new MainStatistics(
                services,
                resumableFunction,
                resumableFunctionsInstances,
                methods,
                pushedCalls,
                latestLogErrors);
        }

        public async Task<List<ServiceInfo>> GetServicesInfo()
        {
            var services = await _context.ServicesData.ToListAsync();
            var counts = await _context
                .MethodIdentifiers
                .GroupBy(x => x.ServiceId)
                .Select(x => new
                {
                    FunctionsCount = x.Count(x => x.Type == Handler.InOuts.MethodType.ResumableFunctionEntryPoint),
                    MethodsCount = x.Count(x => x.Type == Handler.InOuts.MethodType.MethodWait),
                    Id = x.Key
                })
                .ToListAsync();
            var join = from service in services
                       from counter in counts
                       where service.Id == counter.Id
                       select new ServiceInfo(
                           service.Id,
                           service.AssemblyName,
                           service.Url,
                           service.ErrorCounter,
                           counter.FunctionsCount,
                           counter.MethodsCount);
            return join.ToList();
        }
    }
}
