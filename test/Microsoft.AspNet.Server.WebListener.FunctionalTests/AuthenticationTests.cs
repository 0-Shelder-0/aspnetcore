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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.Http.Internal;
using Xunit;
using AuthenticationSchemes = Microsoft.Net.Http.Server.AuthenticationSchemes;

namespace Microsoft.AspNet.Server.WebListener
{
    public class AuthenticationTests
    {
        [Theory]
        [InlineData(AuthenticationSchemes.AllowAnonymous)]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AllowAnonymous_NoChallenge(AuthenticationSchemes authType)
        {
            string address;
            using (Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthType_RequireAuth_ChallengesAdded(AuthenticationSchemes authType)
        {
            string address;
            using (Utilities.CreateHttpAuthServer(authType, out address, httpContext =>
            {
                throw new NotImplementedException();
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthType_AllowAnonymousButSpecify401_ChallengesAdded(AuthenticationSchemes authType)
        {
            string address;
            using (Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                httpContext.Response.StatusCode = 401;
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task MultipleAuthTypes_AllowAnonymousButSpecify401_ChallengesAdded()
        {
            string address;
            using (Utilities.CreateHttpAuthServer(
                AuthenticationSchemes.Negotiate
                | AuthenticationSchemes.NTLM
                /* | AuthenticationSchemes.Digest TODO: Not implemented */
                | AuthenticationSchemes.Basic
                | AuthenticationSchemes.AllowAnonymous,
                out address,
                httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                httpContext.Response.StatusCode = 401;
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal("Negotiate, NTLM, basic", response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /* AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AllowAnonymousButSpecify401_Success(AuthenticationSchemes authType)
        {
            string address;
            int requestId = 0;
            using (Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                if (requestId == 0)
                {
                    Assert.False(httpContext.User.Identity.IsAuthenticated);
                    httpContext.Response.StatusCode = 401;
                }
                else if (requestId == 1)
                {
                    Assert.True(httpContext.User.Identity.IsAuthenticated);
                }
                else
                {
                    throw new NotImplementedException();
                }
                requestId++;
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /* AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_RequireAuth_Success(AuthenticationSchemes authType)
        {
            string address;
            using (Utilities.CreateHttpAuthServer(authType, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.True(httpContext.User.Identity.IsAuthenticated);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.AllowAnonymous)]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthTypes_GetSingleDescriptions(AuthenticationSchemes authType)
        {
            string address;
            using (Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address, httpContext =>
            {
                var resultList = httpContext.Authentication.GetAuthenticationSchemes();
                if (authType == AuthenticationSchemes.AllowAnonymous)
                {
                    Assert.Equal(0, resultList.Count());
                }
                else
                {
                    Assert.Equal(1, resultList.Count());
                    var result = resultList.First();
                    Assert.Equal(authType.ToString(), result.AuthenticationScheme);
                    Assert.Equal("Windows:" + authType.ToString(), result.DisplayName);
                }

                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Fact]
        public async Task AuthTypes_GetMultipleDescriptions()
        {
            string address;
            AuthenticationSchemes authType =
                AuthenticationSchemes.Negotiate
                | AuthenticationSchemes.NTLM
                | /*AuthenticationSchemes.Digest
                |*/ AuthenticationSchemes.Basic;
            using (Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address, httpContext =>
            {
                var resultList = httpContext.Authentication.GetAuthenticationSchemes();
                Assert.Equal(3, resultList.Count());
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AuthenticateWithNoUser_NoResults(AuthenticationSchemes authType)
        {
            string address;
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address, async httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                foreach (var scheme in authTypeList)
                {
                    var authResults = await httpContext.Authentication.AuthenticateAsync(scheme);
                    Assert.Null(authResults);
                }
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AuthenticateWithUser_OneResult(AuthenticationSchemes authType)
        {
            string address;
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (Utilities.CreateHttpAuthServer(authType, out address, async httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.True(httpContext.User.Identity.IsAuthenticated);
                var count = 0;
                foreach (var scheme in authTypeList)
                {
                    var authResults = await httpContext.Authentication.AuthenticateAsync(scheme);
                    if (authResults != null)
                    {
                        count++;
                    }
                }
                Assert.Equal(1, count);
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_ChallengeWithoutAuthTypes_AllChallengesSent(AuthenticationSchemes authType)
        {
            string address;
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                return httpContext.Authentication.ChallengeAsync();
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authTypeList.Count(), response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_ChallengeWithAllAuthTypes_AllChallengesSent(AuthenticationSchemes authType)
        {
            string address;
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address, async httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                foreach (var scheme in authTypeList)
                {
                    await httpContext.Authentication.ChallengeAsync(scheme);
                }
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authTypeList.Count(), response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthTypes_ChallengeOneAuthType_OneChallengeSent(AuthenticationSchemes authType)
        {
            string address;
            var authTypes = AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic;
            using (Utilities.CreateHttpAuthServer(authTypes | AuthenticationSchemes.AllowAnonymous, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                return httpContext.Authentication.ChallengeAsync(authType.ToString());
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(1, response.Headers.WwwAuthenticate.Count);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.First().Scheme);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthTypes_ChallengeDisabledAuthType_Error(AuthenticationSchemes authType)
        {
            string address;
            var authTypes = AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic;
            authTypes = authTypes & ~authType;
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (Utilities.CreateHttpAuthServer(authTypes | AuthenticationSchemes.AllowAnonymous, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                return Assert.ThrowsAsync<InvalidOperationException>(() => httpContext.Authentication.ChallengeAsync(authType.ToString()));
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthTypes_Forbid_Forbidden(AuthenticationSchemes authType)
        {
            string address;
            var authTypes = AuthenticationSchemes.AllowAnonymous | AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic;
            using (Utilities.CreateHttpAuthServer(authTypes, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                return httpContext.Authentication.ForbidAsync(authType.ToString());
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Can't log in with UseDefaultCredentials
        public async Task AuthTypes_ChallengeAuthenticatedAuthType_Forbidden(AuthenticationSchemes authType)
        {
            string address;
            using (Utilities.CreateHttpAuthServer(authType, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.True(httpContext.User.Identity.IsAuthenticated);
                return httpContext.Authentication.ChallengeAsync(authType.ToString());
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                // for some reason Kerberos and Negotiate include a 2nd stage challenge.
                // Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Can't log in with UseDefaultCredentials
        public async Task AuthTypes_ChallengeAuthenticatedAuthTypeWithEmptyChallenge_Forbidden(AuthenticationSchemes authType)
        {
            string address;
            using (Utilities.CreateHttpAuthServer(authType, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.True(httpContext.User.Identity.IsAuthenticated);
                return httpContext.Authentication.ChallengeAsync();
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                // for some reason Kerberos and Negotiate include a 2nd stage challenge.
                // Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Can't log in with UseDefaultCredentials
        public async Task AuthTypes_UnathorizedAuthenticatedAuthType_Unauthorized(AuthenticationSchemes authType)
        {
            string address;
            using (Utilities.CreateHttpAuthServer(authType, out address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.True(httpContext.User.Identity.IsAuthenticated);
                return httpContext.Authentication.ChallengeAsync(authType.ToString(), null, ChallengeBehavior.Unauthorized);
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(1, response.Headers.WwwAuthenticate.Count);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.First().Scheme);
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool useDefaultCredentials = false)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = useDefaultCredentials;
            using (HttpClient client = new HttpClient(handler))
            {
                return await client.GetAsync(uri);
            }
        }
    }
}
