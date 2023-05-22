using AspectInjector.Broker;
using System;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Aspects.PushResult
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [Injection(typeof(PushResultAspect), Inherited = true)]
    public class PushResultAttribute : Attribute
    {

        public PushResultAttribute(string methodUrn, bool canPublishFromExternal = false)
        {
            MethodUrn = methodUrn;
            CanPublishFromExternal = canPublishFromExternal;
        }

        /// <summary>
        /// used to enable developer to change method name an parameters and keep point to the old one
        /// </summary>
        public string MethodUrn { get; }
        public bool CanPublishFromExternal { get; }

        public const string AttributeId = nameof(PushResultAttribute) + "1f220128-d0f7-4dac-ad81-ff942d68942c";
        public override object TypeId => AttributeId;

        public override string ToString()
        {
            return $"{MethodUrn},{CanPublishFromExternal}";
        }
    }
}