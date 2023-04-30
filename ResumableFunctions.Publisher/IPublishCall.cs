using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Publisher
{
    public interface IPublishCall
    {
        Task Publish<TInput, TOutput>(
            Func<TInput, Task<TOutput>> methodToPush,
            TInput input,
            TOutput output,
            string methodIdetifier);
        Task Publish(MethodCall MethodCall);
    }
}
