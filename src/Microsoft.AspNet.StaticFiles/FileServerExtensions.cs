// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.StaticFiles;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods that combine all of the static file middleware components:
    /// Default files, directory browsing, send file, and static files
    /// </summary>
    public static class FileServerExtensions
    {
        /// <summary>
        /// Enable all static file middleware (except directory browsing) for the current request path in the current directory.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseFileServer(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseFileServer(new FileServerOptions());
        }

        /// <summary>
        /// Enable all static file middleware on for the current request path in the current directory.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="enableDirectoryBrowsing">Should directory browsing be enabled?</param>
        /// <returns></returns>
        public static IApplicationBuilder UseFileServer(this IApplicationBuilder builder, bool enableDirectoryBrowsing)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseFileServer(new FileServerOptions() { EnableDirectoryBrowsing = enableDirectoryBrowsing });
        }

        /// <summary>
        /// Enables all static file middleware (except directory browsing) for the given request path from the directory of the same name
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseFileServer(this IApplicationBuilder builder, string requestPath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (requestPath == null)
            {
                throw new ArgumentNullException(nameof(requestPath));
            }

            return builder.UseFileServer(new FileServerOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enable all static file middleware with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseFileServer(this IApplicationBuilder builder, FileServerOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.EnableDefaultFiles)
            {
                builder = builder.UseDefaultFiles(options.DefaultFilesOptions);
            }

            if (options.EnableDirectoryBrowsing)
            {
                builder = builder.UseDirectoryBrowser(options.DirectoryBrowserOptions);
            }

            return builder
                .UseStaticFiles(options.StaticFileOptions);
        }
    }
}