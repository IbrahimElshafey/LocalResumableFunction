using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.Data;
using ResumableFunctions.Handler.InOuts;
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

        public async Task<List<ServiceInfo>> GetServicesList()
        {
            var services = await _context.ServicesData.ToListAsync();
            var childDllsErrors =
                await _context
               .ServicesData
               .GroupBy(x => x.ParentId)
               .Select(x => new { ServiceId = x.Key, ErrorsCount = x.Sum(x => x.ErrorCounter) })
               .ToListAsync();
            var counts = await _context
                .MethodIdentifiers
                .GroupBy(x => x.ServiceId)
                .Select(x => new
                {
                    FunctionsCount = x.Count(x => x.Type == MethodType.ResumableFunctionEntryPoint),
                    MethodsCount = x.Count(x => x.Type == MethodType.MethodWait),
                    Id = x.Key
                })
                .ToListAsync();
            var join = (from service in services
                        from counter in counts
                        from childError in childDllsErrors
                        where service.Id == counter.Id && childError.ServiceId == service.Id
                        select new ServiceInfo(
                            service.Id,
                            service.AssemblyName,
                            service.Url,
                            service.ErrorCounter + childError.ErrorsCount,
                            counter.FunctionsCount,
                            counter.MethodsCount))
                           .ToList();
            return join.ToList();
        }

        public async Task<ServiceStatistics> GetServiceStatistics(int serviceId)
        {
            //name,error counts,functions count,methods count
            var service = await _context.ServicesData.FindAsync(serviceId);
            var counts = await _context
               .MethodIdentifiers
               .Where(x => x.ServiceId == serviceId)
               .GroupBy(x => x.ServiceId)
               .Select(x => new
               {
                   FunctionsCount = x.Count(x => x.Type == Handler.InOuts.MethodType.ResumableFunctionEntryPoint),
                   MethodsCount = x.Count(x => x.Type == Handler.InOuts.MethodType.MethodWait),
                   Id = x.Key
               })
               .FirstOrDefaultAsync();
            return new ServiceStatistics(
                service.Id,
                service.AssemblyName,
                service.ErrorCounter + await _context
                    .ServicesData
                    .Where(x => x.ParentId == serviceId)
                    .Select(x => x.ErrorCounter)
                    .SumAsync(),
                counts?.FunctionsCount ?? 0,
                counts?.MethodsCount ?? 0);
        }

        public async Task<ServiceData> GetServiceInfo(int serviceId)
        {
            var service = await _context.ServicesData.FindAsync(serviceId);
            var dlls = await _context
                .ServicesData
                .Where(x => x.ParentId == serviceId)
                .Select(x => new { x.AssemblyName, x.ErrorCounter })
                .ToListAsync();
            service.ReferencedDlls = dlls.Select(x => x.AssemblyName).ToArray();
            //service.ErrorCounter += dlls.Sum(x => x.ErrorCounter);
            return service;
        }

        public Task<List<LogRecord>> GetServiceLogs(int serviceId)
        {
            return _context
                .Logs
                .Where(x => x.EntityId == serviceId && x.EntityType == nameof(ServiceData))
                .ToListAsync();
        }
    }
}
