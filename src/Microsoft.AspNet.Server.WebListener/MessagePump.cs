// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Server;

namespace Microsoft.AspNet.Server.WebListener
{
    internal class MessagePump : IServer
    {
        private static readonly int DefaultMaxAccepts = 5 * Environment.ProcessorCount;

        private readonly Microsoft.Net.Http.Server.WebListener _listener;
        private readonly ILogger _logger;
        private readonly IHttpContextFactory _httpContextFactory;

        private RequestDelegate _appFunc;

        private int _maxAccepts;
        private int _acceptorCounts;
        private Action<object> _processRequest;

        private bool _stopping;
        private int _outstandingRequests;
        private ManualResetEvent _shutdownSignal;
        
        internal MessagePump(Microsoft.Net.Http.Server.WebListener listener, ILoggerFactory loggerFactory, IFeatureCollection features, IHttpContextFactory httpContextFactory)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(Features));
            }

            Contract.Assert(listener != null);
            _listener = listener;
            _logger = LogHelper.CreateLogger(loggerFactory, typeof(MessagePump));
            _httpContextFactory = httpContextFactory;
            Features = features;

            _processRequest = new Action<object>(ProcessRequestAsync);
            _maxAccepts = DefaultMaxAccepts;
            _shutdownSignal = new ManualResetEvent(false);
        }

        internal Microsoft.Net.Http.Server.WebListener Listener
        {
            get { return _listener; }
        }

        internal int MaxAccepts
        {
            get
            {
                return _maxAccepts;
            }
            set
            {
                _maxAccepts = value;
                if (_listener.IsListening)
                {
                    ActivateRequestProcessingLimits();
                }
            }
        }

        internal bool EnableResponseCaching { get; set; } = true;

        public IFeatureCollection Features { get; }

        public void Start(RequestDelegate app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var addressesFeature = Features.Get<IServerAddressesFeature>();
            if (addressesFeature == null)
            {
                throw new InvalidOperationException($"{nameof(IServerAddressesFeature)} is missing.");
            }

            ParseAddresses(addressesFeature.Addresses, Listener);

            // Can't call Start twice
            Contract.Assert(_appFunc == null);

            Contract.Assert(app != null);

            _appFunc = app;

            if (_listener.UrlPrefixes.Count == 0)
            {
                throw new InvalidOperationException("No address prefixes were defined.");
            }

            _listener.Start();

            ActivateRequestProcessingLimits();
        }

        private void ActivateRequestProcessingLimits()
        {
            for (int i = _acceptorCounts; i < MaxAccepts; i++)
            {
                ProcessRequestsWorker();
            }
        }

        // The message pump.
        // When we start listening for the next request on one thread, we may need to be sure that the
        // completion continues on another thread as to not block the current request processing.
        // The awaits will manage stack depth for us.
        private async void ProcessRequestsWorker()
        {
            int workerIndex = Interlocked.Increment(ref _acceptorCounts);
            while (!_stopping && workerIndex <= MaxAccepts)
            {
                // Receive a request
                RequestContext requestContext;
                try
                {
                    requestContext = await _listener.GetContextAsync().SupressContext();
                }
                catch (Exception exception)
                {
                    Contract.Assert(_stopping);
                    if (_stopping)
                    {
                        LogHelper.LogVerbose(_logger, "ListenForNextRequestAsync-Stopping", exception);
                    }
                    else
                    {
                        LogHelper.LogException(_logger, "ListenForNextRequestAsync", exception);
                    }
                    return;
                }
                try
                {
                    Task ignored = Task.Factory.StartNew(_processRequest, requestContext);
                }
                catch (Exception ex)
                {
                    // Request processing failed to be queued in threadpool
                    // Log the error message, release throttle and move on
                    LogHelper.LogException(_logger, "ProcessRequestAsync", ex);
                }
            }
            Interlocked.Decrement(ref _acceptorCounts);
        }

        private async void ProcessRequestAsync(object requestContextObj)
        {
            var requestContext = requestContextObj as RequestContext;
            try
            {
                if (_stopping)
                {
                    SetFatalResponse(requestContext, 503);
                    return;
                }

                HttpContext httpContext = null;
                try
                {
                    Interlocked.Increment(ref _outstandingRequests);
                    FeatureContext featureContext = new FeatureContext(requestContext, EnableResponseCaching);
                    httpContext = _httpContextFactory.Create(featureContext.Features);
                    await _appFunc(httpContext).SupressContext();
                    requestContext.Dispose();
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(_logger, "ProcessRequestAsync", ex);
                    if (requestContext.Response.HasStartedSending)
                    {
                        requestContext.Abort();
                    }
                    else
                    {
                        // We haven't sent a response yet, try to send a 500 Internal Server Error
                        requestContext.Response.Reset();
                        SetFatalResponse(requestContext, 500);
                    }
                }
                finally
                {
                    if (httpContext != null)
                    {
                        _httpContextFactory.Dispose(httpContext);
                    }
                    if (Interlocked.Decrement(ref _outstandingRequests) == 0 && _stopping)
                    {
                        _shutdownSignal.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(_logger, "ProcessRequestAsync", ex);
                requestContext.Abort();
            }
        }

        private static void SetFatalResponse(RequestContext context, int status)
        {
            context.Response.StatusCode = status;
            context.Response.ContentLength = 0;
            context.Dispose();
        }

        private void ParseAddresses(ICollection<string> addresses, Microsoft.Net.Http.Server.WebListener listener)
        {
            foreach (var value in addresses)
            {
                listener.UrlPrefixes.Add(UrlPrefix.Create(value));
            }
        }


        public void Dispose()
        {
            _stopping = true;
            // Wait for active requests to drain
            if (_outstandingRequests > 0)
            {
                LogHelper.LogInfo(_logger, "Stopping, waiting for " + _outstandingRequests + " request(s) to drain.");
                _shutdownSignal.WaitOne();
            }
            // All requests are finished
            _listener.Dispose();
        }
    }
}
