using Microsoft.EntityFrameworkCore;
using ResumableFunctions.Handler.Data;
using ResumableFunctions.Handler.UiService.InOuts;

namespace ResumableFunctions.Handler.UiService
{
    public class UiService : IUiService
    {
        private readonly FunctionDataContext context;

        public UiService(FunctionDataContext context)
        {
            this.context = context;
        }
        public async Task<MainStatistics> GetMainStatistics()
        {
            var services = await context.ServicesData.CountAsync();
            var resumableFunction = await context.ResumableFunctionIdentifiers.CountAsync();
            var resumableFunctionsInstances = await context.FunctionStates.CountAsync();
            var methods = await context.WaitMethodIdentifiers.CountAsync();
            var pushedCalls = await context.PushedCalls.CountAsync();
            var latestLogErrors =
                await
                context
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
    }
}
