using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.Helpers
{
    public static class ErrorCodes
    {
        public const int MethodValidation = 1;
        public const int Scan = 2;
        public const int ConcurrencyException = 3;
        public const int WaitProcessing = 4;
        public const int Replay = 5;
        public const int PushedCall = 6;
        public const int FirstWait = 7;
        public const int WaitValidation = 8;

        public static Dictionary<int, string> ErrorCodeNames = new Dictionary<int, string>
        {
            {0, null},
            {MethodValidation, "Method Validation"},
            {Scan, "While Scanning Types"},
            {ConcurrencyException, "Concurrency Exception"},
            {WaitProcessing, "Wait Processing"},
            {Replay, "Replay"},
            {PushedCall, "Pushed Call"},
            {FirstWait, "First Wait"},
            {WaitValidation, "Wait Validation"},
        };

        public static string NameOf(int errorCode) => ErrorCodeNames[errorCode];
    }
}
