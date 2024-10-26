﻿using ResumableFunctions.Handler.InOuts;
using ResumableFunctions.Handler.InOuts.Entities;

namespace ResumableFunctions.Handler.Core.Abstraction
{

    //todo: Candidate for MassTransit
    public interface ICallPusher
    {
        Task<long> PushCall(PushedCall pushedCall);
        Task<long> PushExternalCall(PushedCall pushedCall, string serviceName);
    }
}