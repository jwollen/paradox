﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Mono.Options;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Paradox.Engine.Network;

namespace SiliconStudio.Paradox.ConnectionRouter
{
    partial class Program
    {
        private static string IpOverUsbParadoxName = "ParadoxRouterServer";

        private static bool ConsoleVisible = false;

        static int Main(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            var windowsPhonePortMapping = false;
            int exitCode = 0;
            string logFileName = "routerlog.txt";

            var p = new OptionSet
                {
                    "Copyright (C) 2011-2015 Silicon Studio Corporation. All Rights Reserved",
                    "Paradox Router Server - Version: "
                    +
                    String.Format(
                        "{0}.{1}.{2}",
                        typeof(Program).Assembly.GetName().Version.Major,
                        typeof(Program).Assembly.GetName().Version.Minor,
                        typeof(Program).Assembly.GetName().Version.Build) + string.Empty,
                    string.Format("Usage: {0} command [options]*", exeName),
                    string.Empty,
                    "=== Options ===",
                    string.Empty,
                    { "h|help", "Show this message and exit", v => showHelp = v != null },
                    { "log-file=", "Log build in a custom file (default: routerlog.txt).", v => logFileName = v },
                    { "register-windowsphone-portmapping", "Register Windows Phone IpOverUsb port mapping", v => windowsPhonePortMapping = true },
                };

            try
            {
                var commandArgs = p.Parse(args);
                if (showHelp)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    return 0;
                }

                // Make sure path exists
                if (commandArgs.Count > 0)
                    throw new OptionException("This command expect no additional arguments", "");

                if (windowsPhonePortMapping)
                {
                    WindowsPhoneTracker.RegisterWindowsPhonePortMapping();
                    return 0;
                }

                SetupTrayIcon(logFileName);

                // Enable file logging
                if (!string.IsNullOrEmpty(logFileName))
                {
                    var fileLogListener = new TextWriterLogListener(File.Open(logFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                    GlobalLogger.GlobalMessageLogged += fileLogListener;
                }

                try
                {
                    if (!RouterHelper.RouterMutex.WaitOne(TimeSpan.Zero, true))
                    {
                        Console.WriteLine("Another instance of Paradox Router is already running");
                        return -1;
                    }
                }
                catch (AbandonedMutexException)
                {
                    // Previous instance of this application was not closed properly.
                    // However, receiving this exception means we could capture the mutex.
                }

                var router = new Router();

                // Start router (in listen server mode)
                router.Listen(RouterClient.DefaultPort);

                // Start Android management thread
                new Thread(() => AndroidTracker.TrackDevices(router)) { IsBackground = true }.Start();

                // Start Windows Phone management thread
                new Thread(() => WindowsPhoneTracker.TrackDevices(router)) { IsBackground = true }.Start();

                // Start WinForms loop
                System.Windows.Forms.Application.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: {1}", exeName, e);
                if (e is OptionException)
                    p.WriteOptionDescriptions(Console.Out);
                exitCode = 1;
            }

            return exitCode;
        }

        private static string FormatLog(ILogMessage message)
        {
            var builder = new StringBuilder();
            builder.Append("[");
            builder.Append(message.Module);
            builder.Append("] ");
            builder.Append(message.Type.ToString().ToLowerInvariant()).Append(": ");
            builder.Append(message.Text);
            return builder.ToString();
        }

        private static void SetupTrayIcon(string logFileName)
        {
            // Create tray icon
            var components = new System.ComponentModel.Container();

            var notifyIcon = new System.Windows.Forms.NotifyIcon(components);
            notifyIcon.Text = "Paradox Connection Router";
            notifyIcon.Icon = Properties.Resources.Logo;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu();

            if (!string.IsNullOrEmpty(logFileName))
            {
                var showLogMenuItem = new System.Windows.Forms.MenuItem("Show &Log");
                showLogMenuItem.Click += (sender, args) => OnShowLogClick(logFileName);
                notifyIcon.ContextMenu.MenuItems.Add(showLogMenuItem);

                notifyIcon.BalloonTipClicked += (sender, args) => OnShowLogClick(logFileName);
            }

            var openConsoleMenuItem = new System.Windows.Forms.MenuItem("Open Console");
            openConsoleMenuItem.Click += (sender, args) => OnOpenConsoleClick((System.Windows.Forms.MenuItem)sender);
            notifyIcon.ContextMenu.MenuItems.Add(openConsoleMenuItem);

            var exitMenuItem = new System.Windows.Forms.MenuItem("E&xit");
            exitMenuItem.Click += (sender, args) => OnExitClick();
            notifyIcon.ContextMenu.MenuItems.Add(exitMenuItem);

            GlobalLogger.GlobalMessageLogged += (logMessage) =>
            {
                System.Windows.Forms.ToolTipIcon toolTipIcon;
                switch (logMessage.Type)
                {
                    case LogMessageType.Debug:
                    case LogMessageType.Verbose:
                    case LogMessageType.Info:
                        toolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                        break;
                    case LogMessageType.Warning:
                        toolTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
                        break;
                    case LogMessageType.Error:
                    case LogMessageType.Fatal:
                        toolTipIcon = System.Windows.Forms.ToolTipIcon.Error;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Display notification (for one second)
                notifyIcon.ShowBalloonTip(2000, "Paradox Connection Router", logMessage.ToString(), toolTipIcon);
            };

            System.Windows.Forms.Application.ApplicationExit += (sender, e) =>
            {
                notifyIcon.Visible = false;
                notifyIcon.Icon = null;
                notifyIcon.Dispose();
            };
        }

        private static void OnOpenConsoleClick(System.Windows.Forms.MenuItem menuItem)
        {
            menuItem.Enabled = false;

            // Check if not already done
            if (ConsoleVisible)
                return;
            ConsoleVisible = true;

            // Show console
            ConsoleLogListener.ShowConsole();

            // Enable console logging
            var consoleLogListener = new ConsoleLogListener { LogMode = ConsoleLogMode.Always };
            GlobalLogger.GlobalMessageLogged += consoleLogListener;
        }

        private static void OnShowLogClick(string logFileName)
        {
            System.Diagnostics.Process.Start(logFileName);
        }

        private static void OnExitClick()
        {
            System.Windows.Forms.Application.Exit();
        }
    }
}