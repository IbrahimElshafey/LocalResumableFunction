﻿using ResumableFunctions.Core.InOuts;
using MethodBoundaryAspect.Fody.Attributes;
using ResumableFunctions.Core;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace  ResumableFunctions.PublishWebhooks;

/// <summary>
///     Add this to the method you want to 
///     push it's call to the a resumable function service.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]

public sealed class PublishWebhookAttribute : OnMethodBoundaryAspect
{
    private WebhookCall _pushedMethod;
    private ILogger<PublishWebhookAttribute> _logger;
    public PublishWebhookAttribute(string webhookIdetifier)
    {
        if (string.IsNullOrWhiteSpace(webhookIdetifier))
            throw new ArgumentNullException("WebhookIdentifier can't be null or empty.");
        WebhookIdentifier = webhookIdetifier;
        _logger = Extensions.GetServiceProvider().GetService<ILogger<PublishWebhookAttribute>>();
    }

    /// <summary>
    /// used to enable developer to change method name an parameters and keep point to the old one
    /// </summary>
    public string WebhookIdentifier { get; }
    public override object TypeId => nameof(PublishWebhookAttribute);

    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = false;
        _pushedMethod = new WebhookCall
        {
            WebhookIdentifier = WebhookIdentifier
        };
        if (args.Arguments.Length > 0)
            _pushedMethod.Input = args.Arguments[0];
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        try
        {
            _pushedMethod.Output = args.ReturnValue;
            if (args.Method.IsAsyncMethod())
            {
                dynamic output = args.ReturnValue;
                _pushedMethod.Output = output.Result;
            }

            //call `/api/ResumableFunctions/WebHookMethod`
            //_functionHandler.QueuePushedMethodProcessing(_pushedMethod).Wait();
            args.MethodExecutionTag = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when try to pushe method call for method [{args.Method.GetFullName()}]");
        }
    }

    public override void OnException(MethodExecutionArgs args)
    {
        if ((bool)args.MethodExecutionTag)
            return;
        Console.WriteLine("On exception");
    }

    
}
