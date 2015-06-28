using System;
using System.Threading.Tasks;
using SiliconStudio.Paradox.Engine.Network;

namespace SiliconStudio.Paradox.ConnectionRouter
{
    public abstract class RouterServiceServer
    {
        private string address;
        private int port;

        private readonly string serverUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouterServiceServer"/> class.
        /// </summary>
        /// <param name="serverUrl">The URL this service will be advertised as.</param>
        protected RouterServiceServer(string serverUrl)
        {
            this.serverUrl = serverUrl;
        }

        /// <summary>
        /// Tries to connect. Blocks until connection fails or happens (if connection happens, it will launch the message loop in a separate unobserved Task).
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        public async Task TryConnect(string address, int port)
        {
            this.address = address;
            this.port = port;

            var socketContext = CreateSocketContext();

            // Wait for a connection to be possible on adb forwarded port
            await socketContext.StartClient(address, port);
        }

        private SimpleSocket CreateSocketContext()
        {
            var socketContext = new SimpleSocket();
            socketContext.Connected = async (clientSocketContext) =>
            {
                // Register service server
                await socketContext.WriteStream.WriteInt16Async((short)RouterMessage.ServiceProvideServer);
                await socketContext.WriteStream.WriteStringAsync(serverUrl);
                await socketContext.WriteStream.FlushAsync();

                while (true)
                {
                    var routerMessage = (RouterMessage)await socketContext.ReadStream.ReadInt16Async();

                    switch (routerMessage)
                    {
                        case RouterMessage.ServiceRequestServer:
                        {
                            var requestedUrl = await clientSocketContext.ReadStream.ReadStringAsync();
                            var guid = await clientSocketContext.ReadStream.ReadGuidAsync();

                            // Spawn actual server
                            var realServerSocketContext = new SimpleSocket();
                            realServerSocketContext.Connected = async (clientSocketContext2) =>
                            {
                                // Write connection string
                                await clientSocketContext2.WriteStream.WriteInt16Async((short)RouterMessage.ServerStarted);
                                await clientSocketContext2.WriteStream.WriteGuidAsync(guid);

                                // Delegate next steps to actual server
                                HandleClient(clientSocketContext2, requestedUrl);
                            };

                            // Start connection
                            await realServerSocketContext.StartClient(address, port);
                            break;
                        }
                        default:
                            Console.WriteLine("Router: Unknown message: {0}", routerMessage);
                            throw new ArgumentOutOfRangeException();
                    }
                }
            };

            return socketContext;
        }

        /// <summary>
        /// Called when a new client connection has been established.
        /// Before writing anything to the stream, HandleClient is responsible for either calling <see cref="AcceptConnection"/> or <see cref="RefuseConnection"/>.
        /// </summary>
        /// <param name="clientSocket">The client socket.</param>
        /// <param name="url">The requested URL.</param>
        protected abstract void HandleClient(SimpleSocket clientSocket, string url);

        /// <summary>
        /// Let router knows that we want to continue with that connection.
        /// </summary>
        /// <param name="clientSocket">The client socket.</param>
        /// <returns></returns>
        protected async Task AcceptConnection(SimpleSocket clientSocket)
        {
            await clientSocket.WriteStream.WriteInt32Async(0); // error code OK
            await clientSocket.WriteStream.FlushAsync();
        }

        /// <summary>
        /// Let router knows we refuse the connection, and why.
        /// </summary>
        /// <param name="clientSocket">The client socket.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        protected async Task RefuseConnection(SimpleSocket clientSocket, int errorCode, string errorMessage)
        {
            await clientSocket.WriteStream.WriteInt32Async(errorCode);
            await clientSocket.WriteStream.WriteStringAsync(errorMessage);
            await clientSocket.WriteStream.FlushAsync();
        }
    }
}