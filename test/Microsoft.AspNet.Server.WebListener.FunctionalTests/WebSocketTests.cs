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
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Testing.xunit;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    public class WebSocketTests
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Win7And2008R2)]
        public async Task WebSocketTests_SupportKeys_Present()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                try
                {
                    var webSocketFeature = httpContext.GetFeature<IHttpWebSocketFeature>();
                    Assert.NotNull(webSocketFeature);
                }
                catch (Exception ex)
                {
                    return httpContext.Response.WriteAsync(ex.ToString());
                }
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.False(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
                Assert.Equal(0, response.Content.Headers.ContentLength);
                Assert.Equal(string.Empty, response.Content.ReadAsStringAsync().Result);
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Win7And2008R2)]
        public async Task WebSocketTests_AfterHeadersSent_Throws()
        {
            bool? upgradeThrew = null;
            string address;
            using (Utilities.CreateHttpServer(out address, async env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                await httpContext.Response.WriteAsync("Hello World");
                try
                {
                    var webSocketFeature = httpContext.GetFeature<IHttpWebSocketFeature>();
                    Assert.NotNull(webSocketFeature);
                    await webSocketFeature.AcceptAsync(null);
                    upgradeThrew = false;
                }
                catch (InvalidOperationException)
                {
                    upgradeThrew = true;
                }
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.True(upgradeThrew.Value);
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Win7And2008R2)]
        public async Task WebSocketAccept_Success()
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            bool? upgraded = null;
            string address;
            using (Utilities.CreateHttpServer(out address, async env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                var webSocketFeature = httpContext.GetFeature<IHttpWebSocketFeature>();
                Assert.NotNull(webSocketFeature);
                Assert.True(webSocketFeature.IsWebSocketRequest);
                await webSocketFeature.AcceptAsync(null);
                upgraded = true;
                waitHandle.Set();
            }))
            {
                using (WebSocket clientWebSocket = await SendWebSocketRequestAsync(ConvertToWebSocketAddress(address)))
                {
                    Assert.True(waitHandle.WaitOne(TimeSpan.FromSeconds(1)), "Timed out");
                    Assert.True(upgraded.HasValue, "Upgraded not set");
                    Assert.True(upgraded.Value, "Upgrade failed");
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Win7And2008R2)]
        public async Task WebSocketAccept_SendAndReceive_Success()
        {
            byte[] clientBuffer = new byte[] { 0x00, 0x01, 0xFF, 0x00, 0x00 };
            string address;
            using (Utilities.CreateHttpServer(out address, async env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                var webSocketFeature = httpContext.GetFeature<IHttpWebSocketFeature>();
                Assert.NotNull(webSocketFeature);
                Assert.True(webSocketFeature.IsWebSocketRequest);
                var serverWebSocket = await webSocketFeature.AcceptAsync(null);

                byte[] serverBuffer = new byte[clientBuffer.Length];
                var result = await serverWebSocket.ReceiveAsync(new ArraySegment<byte>(serverBuffer, 0, serverBuffer.Length), CancellationToken.None);
                Assert.Equal(clientBuffer, serverBuffer);

                await serverWebSocket.SendAsync(new ArraySegment<byte>(serverBuffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

            }))
            {
                using (WebSocket clientWebSocket = await SendWebSocketRequestAsync(ConvertToWebSocketAddress(address)))
                {
                    await clientWebSocket.SendAsync(new ArraySegment<byte>(clientBuffer, 0, 3), WebSocketMessageType.Binary, true, CancellationToken.None);

                    byte[] clientEchoBuffer = new byte[clientBuffer.Length];
                    var result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(clientEchoBuffer), CancellationToken.None);
                    Assert.Equal(clientBuffer, clientEchoBuffer);
                }
            }
        }

        private string ConvertToWebSocketAddress(string address)
        {
            var builder = new UriBuilder(address);
            builder.Scheme = "ws";
            return builder.ToString();
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }
        
        private async Task<WebSocket> SendWebSocketRequestAsync(string address)
        {
            ClientWebSocket client = new ClientWebSocket();
            await client.ConnectAsync(new Uri(address), CancellationToken.None);
            return client;
        }
    }
}