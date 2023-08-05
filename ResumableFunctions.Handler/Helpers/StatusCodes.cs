using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    public class IgnoreThisField : DefaultContractResolver
    {
        public static IgnoreThisField Instance { get; } = new IgnoreThisField();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (member.Name.EndsWith("__this") || member.Name.Contains("<>"))
            {
                property.Ignored = true;
            }
            return property;
        }
    }
}
