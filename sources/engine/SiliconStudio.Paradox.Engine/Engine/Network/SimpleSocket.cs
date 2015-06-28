﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using Sockets.Plugin;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections;

namespace SiliconStudio.Paradox.Engine.Network
{
    /// <summary>
    /// Manages socket connection+ack and low-level communication.
    /// High-level communication is supposed to happen in <see cref="SocketMessageLayer"/>.
    /// </summary>
    public class SimpleSocket : IDisposable
    {
        private const uint MagicAck = 0x35AABBCC;

        private TcpSocketClient socket;
        private bool isConnected;

        public Stream ReadStream
        {
            get { return socket.ReadStream; }
        }

        public Stream WriteStream
        {
            get { return socket.WriteStream; }
        }

        public string RemoteAddress
        {
            get { return socket.RemoteAddress; }
        }

        public int RemotePort
        {
            get { return socket.RemotePort; }
        }

        // Called on a succesfull connection
        public Action<SimpleSocket> Connected;

        // Called if there is a socket failure (after ack handshake)
        public Action<SimpleSocket> Disconnected;

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeSocket();
        }

        public async Task StartServer(int port, bool singleConnection)
        {
            // Create TCP listener
            var listener = new TcpSocketListener(2048);

            listener.ConnectionReceived = async (sender, args) =>
            {
                var clientSocketContext = new SimpleSocket();

                try
                {
                    // Stop listening if we accept only a single connection
                    if (singleConnection)
                        await listener.StopListeningAsync();

                    clientSocketContext.SetSocket((TcpSocketClient)args.SocketClient);

                    // Do an ack with magic packet (necessary so that we know it's not a dead connection,
                    // it sometimes happen when doing port forwarding because service don't refuse connection right away but only fails when sending data)
                    await SendAndReceiveAck(clientSocketContext.socket, MagicAck, MagicAck);

                    if (Connected != null)
                        Connected(clientSocketContext);

                    clientSocketContext.isConnected = true;
                }
                catch (Exception)
                {
                    clientSocketContext.DisposeSocket();
                }
            };

            // Start listening
            await listener.StartListeningAsync(port);
        }

        public async Task StartClient(string address, int port)
        {
            // Create TCP client
            var socket = new TcpSocketClient(2048);

            try
            {
                await socket.ConnectAsync(address, port);

                SetSocket(socket);
                //socket.NoDelay = true;

                // Do an ack with magic packet (necessary so that we know it's not a dead connection,
                // it sometimes happen when doing port forwarding because service don't refuse connection right away but only fails when sending data)
                await SendAndReceiveAck(socket, MagicAck, MagicAck);

                if (Connected != null)
                    Connected(this);

                isConnected = true;
            }
            catch (Exception)
            {
                DisposeSocket();
                throw;
            }
        }

        private static async Task SendAndReceiveAck(TcpSocketClient socket, uint sentAck, uint expectedAck)
        {
            await socket.WriteStream.WriteInt32Async((int)sentAck);
            await socket.WriteStream.FlushAsync();
            var ack = (uint)await socket.ReadStream.ReadInt32Async();
            if (ack != expectedAck)
                throw new InvalidOperationException("Invalid ack");
        }

        private void SetSocket(TcpSocketClient socket)
        {
            this.socket = socket;
        }

        private void DisposeSocket()
        {
            if (this.socket != null)
            {
                if (isConnected)
                {
                    isConnected = false;
                    if (Disconnected != null)
                        Disconnected(this);
                }

                this.socket.Dispose();
                this.socket = null;
            }
        }
    }
}