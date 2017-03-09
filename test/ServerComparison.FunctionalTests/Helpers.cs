// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.PlatformAbstractions;

namespace ServerComparison.FunctionalTests
{
    public class Helpers
    {
        public static string GetApplicationPath(ApplicationType applicationType)
        {
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;

            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "ServerTests.sln"));
                if (solutionFileInfo.Exists)
                {
                    return Path.GetFullPath(Path.Combine(directoryInfo.FullName, "test", "ServerComparison.TestSites"));
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Solution root could not be found using {applicationBasePath}");
        }

        public static string GetConfigContent(ServerType serverType, string iisConfig, string nginxConfig)
        {
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;

            string content = null;
            if (serverType == ServerType.IISExpress)
            {
                content = File.ReadAllText(Path.Combine(applicationBasePath, iisConfig));
            }
            else if (serverType == ServerType.Nginx)
            {
                content = File.ReadAllText(Path.Combine(applicationBasePath, nginxConfig));
            }

            return content;
        }
    }
}