﻿using System;
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
            {MethodValidation, "Method Validation"},
            {Scanning, "Scanning Types"},
            {WaitProcessing, "Wait Processing"},
            {Replay, "Replay a wait"},
            {PushedCall, "Process Pushed Call"},
            {FirstWait, "First Wait Processing"},
            {WaitValidation, "Wait Request Validation"},
            {Custom, "Author Custom Log"},
            {DataCleaning, "Database Cleaning"},
        };

        public static string NameOf(int errorCode) => StatusCodeNames[errorCode];
    }
}
