using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Publisher
{
    public interface IPublishMethodCall
    {
        Task Publish<TInput, TOutput>(Func<TInput, Task<TOutput>> methodToPush, TInput input, TOutput output);
        Task Publish(MethodCall MethodCall);
    }
}
