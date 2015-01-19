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
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Http.Interfaces;
using Microsoft.AspNet.Testing.xunit;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    public class OpaqueUpgradeTests
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Win7And2008R2)]
        public async Task OpaqueUpgrade_SupportKeys_Present()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                try
                {
                    var opaqueFeature = httpContext.GetFeature<IHttpUpgradeFeature>();
                    Assert.NotNull(opaqueFeature);
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
        public async Task OpaqueUpgrade_AfterHeadersSent_Throws()
        {
            bool? upgradeThrew = null;
            string address;
            using (Utilities.CreateHttpServer(out address, async env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                await httpContext.Response.WriteAsync("Hello World");
                try
                {
                    var opaqueFeature = httpContext.GetFeature<IHttpUpgradeFeature>();
                    Assert.NotNull(opaqueFeature);
                    await opaqueFeature.UpgradeAsync();
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
                Assert.True(response.Headers.TransferEncodingChunked.Value, "Chunked");
                Assert.True(upgradeThrew.Value);
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Win7And2008R2)]
        public async Task OpaqueUpgrade_GetUpgrade_Success()
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            bool? upgraded = null;
            string address;
            using (Utilities.CreateHttpServer(out address, async env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.Headers["Upgrade"] = "websocket"; // Win8.1 blocks anything but WebSockets
                var opaqueFeature = httpContext.GetFeature<IHttpUpgradeFeature>();
                Assert.NotNull(opaqueFeature);
                Assert.True(opaqueFeature.IsUpgradableRequest);
                await opaqueFeature.UpgradeAsync();
                upgraded = true;
                waitHandle.Set();
            }))
            {
                using (Stream stream = await SendOpaqueRequestAsync("GET", address))
                {
                    Assert.True(waitHandle.WaitOne(TimeSpan.FromSeconds(1)), "Timed out");
                    Assert.True(upgraded.HasValue, "Upgraded not set");
                    Assert.True(upgraded.Value, "Upgrade failed");
                }
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Win7And2008R2)]
        // See HTTP_VERB for known verbs
        [InlineData("UNKNOWN", null)]
        [InlineData("INVALID", null)]
        [InlineData("OPTIONS", null)]
        [InlineData("GET", null)]
        [InlineData("HEAD", null)]
        [InlineData("DELETE", null)]
        [InlineData("TRACE", null)]
        [InlineData("CONNECT", null)]
        [InlineData("TRACK", null)]
        [InlineData("MOVE", null)]
        [InlineData("COPY", null)]
        [InlineData("PROPFIND", null)]
        [InlineData("PROPPATCH", null)]
        [InlineData("MKCOL", null)]
        [InlineData("LOCK", null)]
        [InlineData("UNLOCK", null)]
        [InlineData("SEARCH", null)]
        [InlineData("CUSTOMVERB", null)]
        [InlineData("PATCH", null)]
        [InlineData("POST", "Content-Length: 0")]
        [InlineData("PUT", "Content-Length: 0")]
        public async Task OpaqueUpgrade_VariousMethodsUpgradeSendAndReceive_Success(string method, string extraHeader)
        {
            string address;
            using (Utilities.CreateHttpServer(out address, async env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                try
                {
                    httpContext.Response.Headers["Upgrade"] = "websocket"; // Win8.1 blocks anything but WebSockets
                    var opaqueFeature = httpContext.GetFeature<IHttpUpgradeFeature>();
                    Assert.NotNull(opaqueFeature);
                    Assert.True(opaqueFeature.IsUpgradableRequest);
                    var opaqueStream = await opaqueFeature.UpgradeAsync();

                    byte[] buffer = new byte[100];
                    int read = await opaqueStream.ReadAsync(buffer, 0, buffer.Length);

                    await opaqueStream.WriteAsync(buffer, 0, read);
                }
                catch (Exception ex)
                {
                    await httpContext.Response.WriteAsync(ex.ToString());
                }
            }))
            {
                using (Stream stream = await SendOpaqueRequestAsync(method, address, extraHeader))
                {
                    byte[] data = new byte[100];
                    stream.WriteAsync(data, 0, 49).Wait();
                    int read = stream.ReadAsync(data, 0, data.Length).Result;
                    Assert.Equal(49, read);
                }
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Win7And2008R2)]
        // Http.Sys returns a 411 Length Required if PUT or POST does not specify content-length or chunked.
        [InlineData("POST", "Content-Length: 10")]
        [InlineData("POST", "Transfer-Encoding: chunked")]
        [InlineData("PUT", "Content-Length: 10")]
        [InlineData("PUT", "Transfer-Encoding: chunked")]
        [InlineData("CUSTOMVERB", "Content-Length: 10")]
        [InlineData("CUSTOMVERB", "Transfer-Encoding: chunked")]
        public async Task OpaqueUpgrade_InvalidMethodUpgrade_Disconnected(string method, string extraHeader)
        {
            string address;
            using (Utilities.CreateHttpServer(out address, async env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                try
                {
                    var opaqueFeature = httpContext.GetFeature<IHttpUpgradeFeature>();
                    Assert.NotNull(opaqueFeature);
                    Assert.False(opaqueFeature.IsUpgradableRequest);
                }
                catch (Exception ex)
                {
                    await httpContext.Response.WriteAsync(ex.ToString());
                }
            }))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await SendOpaqueRequestAsync(method, address, extraHeader));
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }

        // Returns a bidirectional opaque stream or throws if the upgrade fails
        private async Task<Stream> SendOpaqueRequestAsync(string method, string address, string extraHeader = null)
        {
            // Connect with a socket
            Uri uri = new Uri(address);
            TcpClient client = new TcpClient();
            try
            {
                await client.ConnectAsync(uri.Host, uri.Port);
                NetworkStream stream = client.GetStream();

                // Send an HTTP GET request
                byte[] requestBytes = BuildGetRequest(method, uri, extraHeader);
                await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

                // Read the response headers, fail if it's not a 101
                await ParseResponseAsync(stream);

                // Return the opaque network stream
                return stream;
            }
            catch (Exception)
            {
                client.Close();
                throw;
            }
        }

        private byte[] BuildGetRequest(string method, Uri uri, string extraHeader)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(method);
            builder.Append(" ");
            builder.Append(uri.PathAndQuery);
            builder.Append(" HTTP/1.1");
            builder.AppendLine();

            builder.Append("Host: ");
            builder.Append(uri.Host);
            builder.Append(':');
            builder.Append(uri.Port);
            builder.AppendLine();

            if (!string.IsNullOrEmpty(extraHeader))
            {
                builder.AppendLine(extraHeader);
            }

            builder.AppendLine();
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        // Read the response headers, fail if it's not a 101
        private async Task ParseResponseAsync(NetworkStream stream)
        {
            StreamReader reader = new StreamReader(stream);
            string statusLine = await reader.ReadLineAsync();
            string[] parts = statusLine.Split(' ');
            if (int.Parse(parts[1]) != 101)
            {
                throw new InvalidOperationException("The response status code was incorrect: " + statusLine);
            }

            // Scan to the end of the headers
            while (!string.IsNullOrEmpty(reader.ReadLine()))
            {
            }
        }
    }
}