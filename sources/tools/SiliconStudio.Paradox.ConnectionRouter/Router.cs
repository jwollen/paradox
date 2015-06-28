﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Paradox.Engine.Network;

namespace SiliconStudio.Paradox.ConnectionRouter
{
    public class Router
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("Router");

        private Dictionary<string, TaskCompletionSource<SimpleSocket>> registeredServices = new Dictionary<string, TaskCompletionSource<SimpleSocket>>();
        private Dictionary<Guid, TaskCompletionSource<SimpleSocket>> pendingServers = new Dictionary<Guid, TaskCompletionSource<SimpleSocket>>();

        public void Listen(int port)
        {
            Log.Info("Start to listen on port {0}", port);

            var socketContext = CreateSocketContext();
            Task.Run(() => socketContext.StartServer(port, false));
        }

        /// <summary>
        /// Tries to connect. Blocks until connection fails or happens (if connection happens, it will launch the message loop in a separate unobserved Task).
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        public async Task TryConnect(string address, int port)
        {
            var socketContext = CreateSocketContext();

            // Wait for a connection to be possible on adb forwarded port
            await socketContext.StartClient(address, port);
        }

        private SimpleSocket CreateSocketContext()
        {
            var socketContext = new SimpleSocket();
            socketContext.Connected = async (clientSocketContext) =>
            {
                try
                {
                    // Routing
                    var routerMessage = (RouterMessage)await clientSocketContext.ReadStream.ReadInt16Async();

                    Log.Info("Client {0}:{1} connected, with message {2}", clientSocketContext.RemoteAddress, clientSocketContext.RemotePort, routerMessage);

                    switch (routerMessage)
                    {
                        case RouterMessage.ServiceProvideServer:
                        {
                            await HandleMessageServiceProvideServer(clientSocketContext);
                            break;
                        }
                        case RouterMessage.ServerStarted:
                        {
                            await HandleMessageServerStarted(clientSocketContext);
                            break;
                        }
                        case RouterMessage.ClientRequestServer:
                        {
                            await HandleMessageClientRequestServer(clientSocketContext);
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException(string.Format("Router: Unknown message: {0}", routerMessage));
                    }
                }
                catch (Exception e)
                {
                    // TODO: Ideally, separate socket-related error messages (disconnection) from real errors
                    // Unfortunately, it seems WinRT returns Exception, so it seems we can't filter with SocketException/IOException only?
                    Log.Info("Client {0}:{1} disconnected with exception: {2}", clientSocketContext.RemoteAddress, clientSocketContext.RemotePort, e.Message);
                    clientSocketContext.Dispose();
                }
            };

            return socketContext;
        }

        /// <summary>
        /// Handles ClientRequestServer messages.
        /// It will try to find a matching service (spawn it if not started yet), and ask it to establish a new "server" connection back to us.
        /// </summary>
        /// <param name="clientSocket">The client socket context.</param>
        /// <returns></returns>
        private async Task HandleMessageClientRequestServer(SimpleSocket clientSocket)
        {
            // Check for an existing server
            // TODO: Proper Url parsing (query string)
            var url = await clientSocket.ReadStream.ReadStringAsync();

            string[] urlSegments;
            string urlParameters;
            RouterHelper.ParseUrl(url, out urlSegments, out urlParameters);
            if (urlSegments.Length == 0)
                throw new InvalidOperationException("No URL Segments");

            SimpleSocket serverSocket = null;
            ExceptionDispatchInfo serverSocketCapturedException = null;

            try
            {
                // For now, we handle only "service" URL
                switch (urlSegments[0])
                {
                    case "service":
                    {
                        // From the URL, start service (if not started yet) and ask it to provide a server
                        serverSocket = await SpawnServerFromService(url);
                        break;
                    }
                    default:
                        throw new InvalidOperationException("This type of URL is not supported");
                }
            }
            catch (Exception e)
            {
                serverSocketCapturedException = ExceptionDispatchInfo.Capture(e);
            }


            if (serverSocketCapturedException != null)
            {
                try
                {
                    // Notify client that there was an error
                    await clientSocket.WriteStream.WriteInt16Async((short)RouterMessage.ClientServerStarted);
                    await clientSocket.WriteStream.WriteInt32Async(1); // error code Failure
                    await clientSocket.WriteStream.WriteStringAsync(serverSocketCapturedException.SourceException.Message);
                    await clientSocket.WriteStream.FlushAsync();
                }
                finally
                {
                    serverSocketCapturedException.Throw();
                }
            }

            try
            {
                // Notify client that we've found a server for it
                await clientSocket.WriteStream.WriteInt16Async((short)RouterMessage.ClientServerStarted);
                await clientSocket.WriteStream.WriteInt32Async(0); // error code OK
                await clientSocket.WriteStream.FlushAsync();

                // Let's forward clientSocketContext and serverSocketContext
                await await Task.WhenAny(
                    ForwardSocket(clientSocket, serverSocket),
                    ForwardSocket(serverSocket, clientSocket));
            }
            catch
            {
                serverSocket.Dispose();
                throw;
            }
        }

        private async Task<SimpleSocket> SpawnServerFromService(string url)
        {
            // Ideally we would like to reuse Uri (or some other similar code), but it doesn't work without a Host
            var parameterIndex = url.IndexOf('?');
            var urlWithoutParameters = parameterIndex != -1 ? url.Substring(0, parameterIndex) : url;

            string[] urlSegments;
            string urlParameters;
            RouterHelper.ParseUrl(url, out urlSegments, out urlParameters);

            // Find a matching server
            TaskCompletionSource<SimpleSocket> serviceTCS;

            lock (registeredServices)
            {
                if (!registeredServices.TryGetValue(urlWithoutParameters, out serviceTCS))
                {
                    serviceTCS = new TaskCompletionSource<SimpleSocket>();
                    registeredServices.Add(urlWithoutParameters, serviceTCS);
                }

                if (!serviceTCS.Task.IsCompleted)
                {
                    if (urlSegments.Length < 3)
                    {
                        Log.Error("{0} action URL {1} is invalid", RouterMessage.ClientRequestServer, url);
                        throw new InvalidOperationException();
                    }

                    var paradoxVersion = urlSegments[1];
                    var serviceExe = urlSegments[2];

                    var paradoxSdkDir = RouterHelper.FindParadoxSdkDir(paradoxVersion);
                    if (paradoxSdkDir == null)
                    {
                        Log.Error("{0} action URL {1} references a Paradox version which is not installed", RouterMessage.ClientRequestServer, url);
                        throw new InvalidOperationException();
                    }

                    var servicePath = Path.Combine(paradoxSdkDir, @"Bin\Windows-Direct3D11", serviceExe);
                    RunServiceProcessAndLog(servicePath);
                }
            }

            var service = await serviceTCS.Task;

            // Generate connection Guid
            var guid = Guid.NewGuid();
            var serverSocketTCS = new TaskCompletionSource<SimpleSocket>();
            lock (pendingServers)
            {
                pendingServers.Add(guid, serverSocketTCS);
            }

            // Notify service that we want it to establish back a new connection to us for this client
            await service.WriteStream.WriteInt16Async((short)RouterMessage.ServiceRequestServer);
            await service.WriteStream.WriteStringAsync(url);
            await service.WriteStream.WriteGuidAsync(guid);
            await service.WriteStream.FlushAsync();

            // Should answer within 2 sec
            var ct = new CancellationTokenSource(2000);
            ct.Token.Register(() => serverSocketTCS.TrySetException(new TimeoutException("Server could not connect back in time")));

            // Wait for such a server to be available
            return await serverSocketTCS.Task;
        }

        private static Process RunServiceProcessAndLog(string servicePath)
        {
            var process = ShellHelper.RunProcess(servicePath, string.Empty);

            // Create log and notify start
            var logModule = string.Format("{0}:{1}", Path.GetFileNameWithoutExtension(servicePath), process.Id);
            var logger = GlobalLogger.GetLogger(logModule);
            logger.Info("Process started");

            process.OutputDataReceived += (_, args) => logger.Info(args.Data);
            process.ErrorDataReceived += (_, args) => logger.Error(args.Data);
            process.Exited += (_, args) => logger.Info("Process exited");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Let's tie lifetime of spawned process to ours
            // TODO: Move that in a better namespace? (currently a shared file)
            new GameStudio.Plugin.Debugging.AttachedChildProcessJob(process);

            return process;
        }

        /// <summary>
        /// Handles ServerStarted messages. It happens when service opened a new "server" connection back to us.
        /// </summary>
        /// <param name="clientSocket">The client socket context.</param>
        /// <returns></returns>
        private async Task HandleMessageServerStarted(SimpleSocket clientSocket)
        {
            var guid = await clientSocket.ReadStream.ReadGuidAsync();
            var errorCode = await clientSocket.ReadStream.ReadInt32Async();
            var errorMessage = (errorCode != 0) ? await clientSocket.ReadStream.ReadStringAsync() : null;

            // Notify any waiter that a server with given GUID is available
            TaskCompletionSource<SimpleSocket> serverSocketTCS;
            lock (pendingServers)
            {
                if (!pendingServers.TryGetValue(guid, out serverSocketTCS))
                {
                    Log.Error("Could not find a matching server Guid");
                    clientSocket.Dispose();
                    return;
                }

                pendingServers.Remove(guid);
            }

            if (errorCode != 0)
                serverSocketTCS.TrySetException(new Exception(errorMessage));
            else
                serverSocketTCS.TrySetResult(clientSocket);
        }

        /// <summary>
        /// Handles ServiceProvideServer messages. It allows service to publicize what "server" they can instantiate.
        /// </summary>
        /// <param name="clientSocket">The client socket context.</param>
        /// <returns></returns>
        private async Task HandleMessageServiceProvideServer(SimpleSocket clientSocket)
        {
            var url = await clientSocket.ReadStream.ReadStringAsync();
            TaskCompletionSource<SimpleSocket> service;

            lock (registeredServices)
            {
                if (!registeredServices.TryGetValue(url, out service))
                {
                    service = new TaskCompletionSource<SimpleSocket>();
                    registeredServices.Add(url, service);
                }

                service.TrySetResult(clientSocket);
            }

            // TODO: Handle server disconnections
            //clientSocketContext.Disconnected += 
        }

        private async Task ForwardSocket(SimpleSocket source, SimpleSocket target)
        {
            var buffer = new byte[1024];
            while (true)
            {
                var bufferLength = await source.ReadStream.ReadAsync(buffer, 0, buffer.Length);
                if (bufferLength == 0)
                    throw new IOException("Socket closed");
                await target.WriteStream.WriteAsync(buffer, 0, bufferLength);
                await target.WriteStream.FlushAsync();
            }
        }
    }
}