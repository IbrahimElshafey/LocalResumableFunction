using System.Diagnostics;
using System.Reflection;
using LocalResumableFunction;
using LocalResumableFunction.Data;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace ResumableFunctionScanner;

internal partial class Scanner
{
    //todo:move to handler
    private async Task RegisterResumableFunctionFirstWait(MethodInfo resumableFunction)
    {
        WriteMessage("START RESUMABLE FUNCTION AND REGISTER FIRST WAIT");
        var handler = new ResumableFunctionHandler(_context);
        await handler.RegisterFirstWait(resumableFunction);
    }

    
}