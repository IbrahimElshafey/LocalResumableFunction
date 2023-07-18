using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.Helpers
{
    public static class StatusCodes
    {
        public const int MethodValidation = 1;
        public const int Scanning = 2;
        public const int WaitProcessing = 4;
        public const int Replay = 5;
        public const int PushedCall = 6;
        public const int FirstWait = 7;
        public const int WaitValidation = 8;
        public const int DataCleaning = 9;
        public const int Custom = -1000;

        public static Dictionary<int, string> StatusCodeNames = new Dictionary<int, string>
        {
            {-1, "Any"},
            {MethodValidation, "While Method Validation"},
            {Scanning, "While Scanning Types"},
            {WaitProcessing, "While Wait Processing"},
            {Replay, "While Replay a wait"},
            {PushedCall, "When Process Pushed Call"},
            {FirstWait, "While First Wait Processing"},
            {WaitValidation, "While Wait Request Validation"},
            {Custom, "Author Custom Log"},
            {DataCleaning, "Database Cleaning"},
        };

        public static string NameOf(int errorCode) => StatusCodeNames[errorCode];
    }
}
