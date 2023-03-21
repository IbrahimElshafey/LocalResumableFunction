using ResumableFunctions.Core.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Core.Abstraction
{

    public interface IPushMethodCall
    {
        void MethodCalled(PushedMethod pushedMethod);
    }
}
