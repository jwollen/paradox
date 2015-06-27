﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using Mono.Options;
using SiliconStudio.Core;
using SiliconStudio.Paradox.Debugger.Target;

namespace SiliconStudio.Paradox
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;

            string hostPipe = null;
            bool waitDebuggerAttach = false;

            var p = new OptionSet
            {
                "Copyright (C) 2015 Silicon Studio Corporation. All Rights Reserved",
                "Paradox Debugger Host tool - Version: "
                +
                String.Format(
                    "{0}.{1}.{2}",
                    typeof(Program).Assembly.GetName().Version.Major,
                    typeof(Program).Assembly.GetName().Version.Minor,
                    typeof(Program).Assembly.GetName().Version.Build) + string.Empty,
                string.Format("Usage: {0} --host=[hostpipe]", exeName),
                string.Empty,
                "=== Options ===",
                string.Empty,
                { "h|help", "Show this message and exit", v => showHelp = v != null },
                { "host=", "Host pipe", v => hostPipe = v },
                { "wait-debugger-attach", "Process will wait for a debuggger to attach, for 5 seconds", v => waitDebuggerAttach = true },
            };

            try
            {
                var unexpectedArgs = p.Parse(args);
                if (unexpectedArgs.Any())
                {
                    throw new OptionException("Unexpected arguments [{0}]".ToFormat(string.Join(", ", unexpectedArgs)), "args");
                }

                if (waitDebuggerAttach)
                {
                    // Wait for 2 second max
                    for (int i = 0; i < 500; ++i)
                    {
                        if (System.Diagnostics.Debugger.IsAttached)
                        {
                            break;
                        }

                        Thread.Sleep(10);
                    }
                }

                if (hostPipe == null)
                {
                    throw new OptionException("Host pipe not specified", "host");
                }

                // Open WCF channel with master builder
                var namedPipeBinding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { SendTimeout = TimeSpan.FromSeconds(300.0) };
                try
                {
                    var gameDebuggerTarget = new GameDebuggerTarget();
                    var gameDebuggerHost = DuplexChannelFactory<IGameDebuggerHost>.CreateChannel(new InstanceContext(gameDebuggerTarget), namedPipeBinding, new EndpointAddress(hostPipe));
                    gameDebuggerTarget.MainLoop(gameDebuggerHost);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine("Command option '{0}': {1}", e.OptionName, e.Message);
                return -1;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unhandled exception: {0}", e);
                return -1;
            }

            return 0;
        }
    }
}