using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumableFunctions.Handler.Helpers
{
    public class Constants
    {
        public const string MethodMustNotBeGeneric = "MethodMustNotBeGeneric";
        public const string MethodMustReturnValue = "MethodMustReturnValue";
        public const string AsyncMethodMustBeTask = "AsyncMethodMustBeTask";
        public const string MethodMustBeInstance = "MethodMustBeInstance";
        public const string MethodMustHaveOneInput = "MethodMustHaveOneInput";
        public const string DllExistInAnotherService = "DllExistInAnotherService";
        public const string DllNotReferenceRequiredDll = "DllNotReferenceRequiredDll";
        public const string CantRegisterFunction = "CantRegisterFunction";
        public const string FunctionMustBeAsync = "FunctionMustBeAsync";
        public const string FunctionNotMatchSignature = "FunctionNotMatchSignature";
        public const string FileNotExist = "FileNotExist";
        public const string MatchEvaluationError = "MatchEvaluationError";
        public const string ConcurrencyException = "ConcurrencyException";
        public const string ProceedToNextWaitParentNull = "ProceedToNextWaitParentNull";
        public const string ProceedToNextWaitError = "ProceedToNextWaitError";
        public const string ReplayWaitMustBeMethod = "ReplayWaitMustBeMethod";
        public const string ReplayFirstError = "ReplayFirstError";
        public const string ReplayNoWaitFound = "ReplayNoWaitFound";
        public const string SetDataEvaluationError = "SetDataEvaluationError";
        public const string MethodNotInCode = "MethodNotInCode";

        public static List<string> ErrorCodes = new List<string>
        {
            MethodMustNotBeGeneric      ,
            MethodMustReturnValue       ,
            AsyncMethodMustBeTask       ,
            MethodMustBeInstance        ,
            MethodMustHaveOneInput      ,
            DllExistInAnotherService    ,
            DllNotReferenceRequiredDll  ,
            CantRegisterFunction        ,
            FunctionMustBeAsync         ,
            FunctionNotMatchSignature   ,
            FileNotExist                ,
            MatchEvaluationError        ,
            ConcurrencyException        ,
            ProceedToNextWaitParentNull ,
            ProceedToNextWaitError      ,
            ReplayWaitMustBeMethod      ,
            ReplayFirstError            ,
            ReplayNoWaitFound           ,
            SetDataEvaluationError      ,
            MethodNotInCode             ,
        };
    }
}
