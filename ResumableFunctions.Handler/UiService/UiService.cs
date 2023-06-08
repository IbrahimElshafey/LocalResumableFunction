using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.DataAccess;
using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.UiService.InOuts;
using System;
using System.Linq;

namespace ResumableFunctions.Handler.UiService
{
    internal class UiService : IUiService
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
            var result = new List<ServiceInfo>();
            var services = await
                _context
                .ServicesData
                .Where(x => x.ParentId == -1)
                .ToListAsync();
            var childDllsErrors =
                await _context
               .ServicesData
               .Where(x => x.ParentId != -1)
               .GroupBy(x => x.ParentId)
               .Select(x => new { ServiceId = x.Key, ErrorsCount = x.Sum(x => x.ErrorCounter) })
               .ToDictionaryAsync(x => x.ServiceId);
            var counts = await _context
                .MethodIdentifiers
                .GroupBy(x => x.ServiceId)
                .Select(x => new
                {
                    FunctionsCount = x.Count(x => x.Type == MethodType.ResumableFunctionEntryPoint),
                    MethodsCount = x.Count(x => x.Type == MethodType.MethodWait),
                    ServiceId = x.Key
                })
                .ToDictionaryAsync(x => x.ServiceId);
            foreach (var service in services)
            {
                var item = new ServiceInfo(service.Id, service.AssemblyName, service.Url, service.ReferencedDlls, service.Created, service.Modified);
                item.LogErrors = service.ErrorCounter;
                if (childDllsErrors.ContainsKey(service.Id))
                    item.LogErrors += childDllsErrors[service.Id].ErrorsCount;
                if (counts.ContainsKey(service.Id))
                {
                    var methodsCounter = counts[service.Id];
                    item.FunctionsCount = methodsCounter.FunctionsCount;
                    item.MethodsCount = methodsCounter.MethodsCount;
                }
                result.Add(item);
            }
            return result;
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

        public async Task<List<FunctionInfo>> GetFunctionsInfo(int? serviceId)
        {
            return await _context.ResumableFunctionIdentifiers
              .Include(x => x.ActiveFunctionsStates)
              .Include(x => x.WaitsCreatedByFunction)
              .Where(x => x.Type == MethodType.ResumableFunctionEntryPoint)
              .Select(x => new FunctionInfo(
                      x,
                      x.WaitsCreatedByFunction.First(x => x.IsFirst && x.IsNode).Name,
                      x.ActiveFunctionsStates.Count(x => x.Status == FunctionStatus.InProgress),
                      x.ActiveFunctionsStates.Count(x => x.Status == FunctionStatus.Completed),
                      x.ActiveFunctionsStates.Count(x => x.Status == FunctionStatus.Error)))
              .ToListAsync();
        }

        public async Task<List<MethodGroupInfo>> GetMethodsInfo(int? serviceId)
        {
            // int Id, string URN, int MethodsCount,int ActiveWaits,int CompletedWaits,int CanceledWaits
            var waitsQuery = _context
                .MethodWaits
                .GroupBy(x => x.MethodGroupToWaitId)
                .Select(x => new
                {
                    Waiting = x.Count(x => x.Status == WaitStatus.Waiting),
                    Completed = x.Count(x => x.Status == WaitStatus.Completed),
                    Canceled = x.Count(x => x.Status == WaitStatus.Canceled),
                    //LastWait = x.Max(x => x.Created),
                    MethodGroupId = x.Key
                });
            //todo refine this query
            var methodIdsQuery = _context
                .WaitMethodIdentifiers
                .GroupBy(x => x.ParentMethodGroupId)
                .Select(x => new
                {
                    MethodGroupId = x.Key,
                    MethodsCount = x.Count(),
                    GroupCreated = x.First().ParentMethodGroup.Created,
                    GroupUrn = x.First().ParentMethodGroup.MethodGroupUrn
                });
            var join = from wait in waitsQuery
                       from methodId in methodIdsQuery
                       where wait.MethodGroupId == methodId.MethodGroupId
                       select new { wait, methodId };

            return (await join.ToListAsync())
                .Select(x => new MethodGroupInfo(
                                     x.wait.MethodGroupId,
                                     x.methodId.GroupUrn,
                                     x.methodId.MethodsCount,
                                     x.wait.Waiting,
                                     x.wait.Completed,
                                     x.wait.Canceled,
                                     x.methodId.GroupCreated))
                .ToList();
        }

        public async Task<List<PushedCallInfo>> GetPushedCalls(int page = 0)
        {
            //x.WaitsForCall.Count(),
            //x.WaitsForCall.Count(x => x.Status == WaitForCallStatus.Matched),
            //x.WaitsForCall.Count(x => x.Status == WaitForCallStatus.NotMatched)
            var counts =
                _context
                .WaitsForCalls
                .GroupBy(x => x.PushedCallId)
                .Select(x => new
                {
                    CallId = x.Key,
                    All = x.Count(),
                    Matched = x.Count(x => x.Status == WaitForCallStatus.Matched),
                    NotMatched = x.Count(x => x.Status == WaitForCallStatus.NotMatched),
                });
            var query = _context.PushedCalls
                .Join(counts,
                call => call.Id,
                counter => counter.CallId,
                (call, counter) =>
                new PushedCallInfo(
                    call,
                    counter.All,
                    counter.Matched,
                    counter.NotMatched
                ));

            var result = await query.ToListAsync();
            result.ForEach(x => x.PushedCall.LoadUnmappedProps());
            return result;
        }

        public async Task<List<FunctionInstanceInfo>> GetFunctionInstances(int functionId)
        {
            var query =
                 _context.FunctionStates
                 .Where(x => x.ResumableFunctionIdentifierId == functionId)
                 .Include(x => x.Waits)
                 .Select(functionState => new FunctionInstanceInfo(
                     functionState,
                     functionState.Waits.First(wait => wait.IsNode && wait.Status == WaitStatus.Waiting),
                     functionState.Waits.Count()));
            var result = await query.ToListAsync();
            result.ForEach(x => x.FunctionState.LoadUnmappedProps());
            return result;
        }
    }
}
