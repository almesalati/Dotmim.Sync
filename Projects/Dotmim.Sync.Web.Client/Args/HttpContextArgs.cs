﻿using Dotmim.Sync.Enumerations;
using Dotmim.Sync.Web.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dotmim.Sync
{

    /// <summary>
    /// When Getting response from remote orchestrator
    /// </summary>
    public class HttpGettingResponseMessageArgs : ProgressArgs
    {
        public HttpGettingResponseMessageArgs(HttpResponseMessage response, SyncContext context)
            : base(context, null, null)
        {
            this.Response = response;
        }
        public override int EventId => HttpClientSyncEventsId.HttpGettingResponseMessage.Id;
        public override SyncProgressLevel ProgressLevel => SyncProgressLevel.Debug;

        public HttpResponseMessage Response { get; }
    }

    /// <summary>
    /// When sending request 
    /// </summary>
    public class HttpSendingRequestMessageArgs : ProgressArgs
    {
        public HttpSendingRequestMessageArgs(HttpRequestMessage request, SyncContext context)
            : base(context, null, null)
        {
            this.Request = request;
        }
        public override int EventId => HttpClientSyncEventsId.HttpSendingRequestMessage.Id;
        public override SyncProgressLevel ProgressLevel => SyncProgressLevel.Debug;

        public HttpRequestMessage Request { get; }
    }

    public static partial class HttpClientSyncEventsId
    {
        public static EventId HttpSendingRequestMessage => new EventId(20100, nameof(HttpSendingRequestMessage));
        public static EventId HttpGettingResponseMessage => new EventId(20150, nameof(HttpGettingResponseMessage));
    }

    /// <summary>
    /// Partial interceptors extensions 
    /// </summary>
    public static partial class HttpInterceptorsExtensions
    {
        /// <summary>
        /// Intercept the provider when an http request message is sent
        /// </summary>
        public static Guid OnHttpSendingRequest(this WebRemoteOrchestrator orchestrator, 
            Action<HttpSendingRequestMessageArgs> action)
            => orchestrator.AddInterceptor(action);
        /// <summary>
        /// Intercept the provider when an http request message is sent
        /// </summary>
        public static Guid OnHttpSendingRequest(this WebRemoteOrchestrator orchestrator, 
            Func<HttpSendingRequestMessageArgs, Task> action)
            => orchestrator.AddInterceptor(action);

        /// <summary>
        /// Intercept the provider when an http message response is downloaded from remote side
        /// </summary>
        public static Guid OnHttpGettingResponse(this WebRemoteOrchestrator orchestrator, 
            Action<HttpGettingResponseMessageArgs> action)
            => orchestrator.AddInterceptor(action);
        /// <summary>
        /// Intercept the provider when an http message response is downloaded from remote side
        /// </summary>
        public static Guid OnHttpGettingResponse(this WebRemoteOrchestrator orchestrator, 
            Func<HttpGettingResponseMessageArgs, Task> action)
            => orchestrator.AddInterceptor(action);

    }
}
