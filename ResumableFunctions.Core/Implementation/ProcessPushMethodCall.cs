using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using ResumableFunctions.Core.Abstraction;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Core.Implementation
{
    internal class ProcessPushedMethodCall : IProcessPushedMethodCall
    {
        private readonly IServiceProvider _serviceProvider;

        public ProcessPushedMethodCall(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task ProcessPushedMethod(PushedMethod pushedMethod)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var _handler = _serviceProvider.GetService<ResumableFunctionHandler>();
                await _handler.ProcessPushedMethod(pushedMethod);
            }
        }
    }
}
