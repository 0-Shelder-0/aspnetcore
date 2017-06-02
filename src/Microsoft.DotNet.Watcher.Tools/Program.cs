﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher
{
    public class Program : IDisposable
    {
        private readonly IConsole _console;
        private readonly string _workingDir;
        private readonly CancellationTokenSource _cts;
        private IReporter _reporter;

        public Program(IConsole console, string workingDir)
        {
            Ensure.NotNull(console, nameof(console));
            Ensure.NotNullOrEmpty(workingDir, nameof(workingDir));

            _console = console;
            _workingDir = workingDir;
            _cts = new CancellationTokenSource();
            _console.CancelKeyPress += OnCancelKeyPress;
            _reporter = CreateReporter(verbose: true, quiet: false, console: _console);
        }

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);
            using (var program = new Program(PhysicalConsole.Singleton, Directory.GetCurrentDirectory()))
            {
                return program
                    .RunAsync(args)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        public async Task<int> RunAsync(string[] args)
        {
            CommandLineOptions options;
            try
            {
                options = CommandLineOptions.Parse(args, _console);
            }
            catch (CommandParsingException ex)
            {
                _reporter.Error(ex.Message);
                return 1;
            }

            if (options == null)
            {
                // invalid args syntax
                return 1;
            }

            if (options.IsHelp)
            {
                return 2;
            }

            // update reporter as configured by options
            _reporter = CreateReporter(options.IsVerbose, options.IsQuiet, _console);

            try
            {
                if (_cts.IsCancellationRequested)
                {
                    return 1;
                }

                if (options.ListFiles)
                {
                    return await ListFilesAsync(_reporter,
                        options.Project,
                        options.MSBuildProjectExtensionsPath,
                        _cts.Token);
                }
                else
                {
                    return await MainInternalAsync(_reporter,
                        options.Project,
                        options.MSBuildProjectExtensionsPath,
                        options.RemainingArguments,
                        _cts.Token);
                }
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    // swallow when only exception is the CTRL+C forced an exit
                    return 0;
                }

                _reporter.Error(ex.ToString());
                _reporter.Error("An unexpected error occurred");
                return 1;
            }
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            // suppress CTRL+C on the first press
            args.Cancel = !_cts.IsCancellationRequested;

            if (args.Cancel)
            {
                _reporter.Output("Shutdown requested. Press Ctrl+C again to force exit.");
            }

            _cts.Cancel();
        }

        private async Task<int> MainInternalAsync(
            IReporter reporter,
            string project,
            string msbuildProjectExtensionsPath,
            ICollection<string> args,
            CancellationToken cancellationToken)
        {
            // TODO multiple projects should be easy enough to add here
            string projectFile;
            try
            {
                projectFile = MsBuildProjectFinder.FindMsBuildProject(_workingDir, project);
            }
            catch (FileNotFoundException ex)
            {
                reporter.Error(ex.Message);
                return 1;
            }

            var fileSetFactory = new MsBuildFileSetFactory(reporter,
                projectFile,
                NormalizePath(msbuildProjectExtensionsPath),
                waitOnError: true);
            var processInfo = new ProcessSpec
            {
                Executable = DotNetMuxer.MuxerPathOrDefault(),
                WorkingDirectory = Path.GetDirectoryName(projectFile),
                Arguments = args,
                EnvironmentVariables =
                {
                    ["DOTNET_WATCH"] = "1"
                },
            };

            await new DotNetWatcher(reporter)
                .WatchAsync(processInfo, fileSetFactory, cancellationToken);

            return 0;
        }

        private async Task<int> ListFilesAsync(
            IReporter reporter,
            string project,
            string msbuildProjectExtensionsPath,
            CancellationToken cancellationToken)
        {
            // TODO multiple projects should be easy enough to add here
            string projectFile;
            try
            {
                projectFile = MsBuildProjectFinder.FindMsBuildProject(_workingDir, project);
            }
            catch (FileNotFoundException ex)
            {
                reporter.Error(ex.Message);
                return 1;
            }

            var fileSetFactory = new MsBuildFileSetFactory(reporter,
                projectFile,
                NormalizePath(msbuildProjectExtensionsPath),
                waitOnError: false);
            var files = await fileSetFactory.CreateAsync(cancellationToken);

            if (files == null)
            {
                return 1;
            }

            foreach (var file in files)
            {
                _console.Out.WriteLine(file);
            }

            return 0;
        }

        private static IReporter CreateReporter(bool verbose, bool quiet, IConsole console)
            => new PrefixConsoleReporter(console, verbose || CliContext.IsGlobalVerbose(), quiet);


        private string NormalizePath(string path)
        {
            if (path == null || Path.IsPathRooted(path))
            {
                return path;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return _workingDir;
            }

            return Path.Combine(_workingDir, path);
        }

        public void Dispose()
        {
            _console.CancelKeyPress -= OnCancelKeyPress;
            _cts.Dispose();
        }
    }
}
