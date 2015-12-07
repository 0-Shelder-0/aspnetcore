﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.IISPlatformHandler
{
    public class HttpPlatformHandlerMiddlewareTests
    {
        [Fact]
        public async Task AddsAuthenticationHandlerByDefault()
        {
            var assertsExecuted = false;

            var server = TestServer.Create(app =>
            {
                app.UseIISPlatformHandler();
                app.Run(context =>
                {
                    var auth = (IHttpAuthenticationFeature)context.Features[typeof(IHttpAuthenticationFeature)];
                    Assert.NotNull(auth);
                    Assert.Equal("Microsoft.AspNet.IISPlatformHandler.AuthenticationHandler", auth.Handler.GetType().FullName);
                    assertsExecuted = true;
                    return Task.FromResult(0);
                });
            });

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }


        [Fact]
        public async Task DoesNotAddAuthenticationHandlerIfWindowsAuthDisabled()
        {
            var assertsExecuted = false;

            var server = TestServer.Create(app =>
            {
                app.UseIISPlatformHandler(options => options.FlowWindowsAuthentication = false);
                app.Run(context =>
                {
                    var auth = (IHttpAuthenticationFeature)context.Features[typeof(IHttpAuthenticationFeature)];
                    Assert.Null(auth);
                    assertsExecuted = true;
                    return Task.FromResult(0);
                });
            });

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }
    }
}
