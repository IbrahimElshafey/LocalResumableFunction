using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.AspNetService.DisplayObject
{
    internal class LogsViewModel
    {
        public List<LogRecord> Logs { get; }
        public Dictionary<int, string> ErrorCodes => Handler.Helpers.ErrorCodes.ErrorCodeNames;
        public List<ServiceData> Services { get; }
    }
}
