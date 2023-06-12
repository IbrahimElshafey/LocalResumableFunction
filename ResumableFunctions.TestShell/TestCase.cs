using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.TestShell
{
    public class TestCase
    {
        private HostApplicationBuilder builder;

        public TestCase(params Type[] types)
        {
            builder = Host.CreateApplicationBuilder();
            builder.Services.AddResumableFunctionsCore();
        }

        public void Start()
        {
            var app = builder.Build();
            app.UseResumableFunctions();
            app.Run();
        }

        public void SimulateMethodCall<ClassType, Input, Output>(
            Expression<Func<ClassType, object>> methodSelector,
            Input input, 
            Output outPut)
        {

        }
    }
}
