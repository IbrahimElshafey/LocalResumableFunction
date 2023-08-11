﻿using AspectInjector.Broker;
using System;

namespace ResumableFunctions.Publisher.Helpers
{
    /// <summary>
    ///     Add this to the method you want to 
    ///     push it's call to the a resumable function service.
    /// </summary>  
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [Injection(typeof(PublishMethodAspect), Inherited = true)]
    public sealed class PublishMethodAttribute : Attribute
    {
        public PublishMethodAttribute(string methodUrn)
        {
            MethodUrn = methodUrn;
        }

        /// <summary>
        /// used to enable developer to change method name an parameters and keep point to the old one
        /// </summary>
        public string MethodUrn { get; }
        public string ToService { get; set; }
        public override object TypeId => "c0a6b0c2-c79f-427b-a66a-8df59076e3ff";
    }
}