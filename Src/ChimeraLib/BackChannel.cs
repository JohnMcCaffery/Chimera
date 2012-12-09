﻿/*************************************************************************
Copyright (c) 2012 John McCaffery 

This file is part of Chimera.

Chimera is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Chimera is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Chimera.  If not, see <http://www.gnu.org/licenses/>.

**************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using OpenMetaverse.Packets;
using OpenMetaverse;
using System.Threading;
using GridProxy;
using log4net;
using System.Net.NetworkInformation;

namespace UtilLib {
    public abstract class BackChannel {
        public readonly static string PING = "Ping";

        public readonly static string CONNECT = "Connect";

        public readonly static string DISCONNECT = "Disconnect";

        public readonly static string ACCEPT = "Accept";

        public readonly static string REJECT = "Reject";

        public readonly static byte[] PING_B = Encoding.ASCII.GetBytes(PING);

        public readonly static byte[] CONNECT_B = Encoding.ASCII.GetBytes(CONNECT);

        public readonly static byte[] DISCONNECT_B = Encoding.ASCII.GetBytes(DISCONNECT);

        public readonly static byte[] ACCEPT_B = Encoding.ASCII.GetBytes(ACCEPT);

        public readonly static byte[] REJECT_B = Encoding.ASCII.GetBytes(REJECT);

        private readonly List<IPAddress> localAddresses = new List<IPAddress>();

        private ILog logger;

        /// <summary>
        /// Mapping of listeners for every different packet received.
        /// </summary>
        private readonly Dictionary<string, MessageDelegate> packetDelegates = new Dictionary<string,MessageDelegate>();

        /// <summary>
        /// UdpClient to send and receive packets from.
        /// </summary>
        private UdpClient socket;

        /// <summary>
        /// UdpClient to send ping packets which will check whether a connection is still alive.
        /// </summary>
        private UdpClient testConnectionSocket;

        /// <summary>
        /// Tracks whether the socket is currently bound.
        /// </summary>
        private bool bound;

        /// <summary>
        /// Buffer for zero coded packets.
        /// </summary>
        private byte[] zeroBuffer = new byte[8192];

        /// <summary>
        /// How many packets the slave has received.
        /// </summary>
        private int receivedPackets = 0;

        /// <summary>
        /// The address to bind to. Default = "localhost".
        /// </summary>
        private string address = Dns.GetHostName();

        /// <summary>
        /// The port to bind to. Default = 0.
        /// </summary>
        private int port = 0;

        protected IPAddress AsLocalIP(string address) {
            return localAddresses.FirstOrDefault(addr => addr.ToString().Equals(address));
        }

        protected IPAddress GetLocal() {
            if (localAddresses.Count == 0)
                return IPAddress.Loopback;
            return localAddresses.First();
        }

        /// <summary>
        /// Triggered whenever the back channel binds itself to a port.
        /// </summary>
        public event EventHandler OnBound;

        /// <summary>
        /// Triggered whenever a new datagram packet is received.
        /// </summary>
        public event DataDelegate OnDataReceived;

        /// <summary>
        /// Triggered whenever a new packet is received.
        /// </summary>
        public event PacketDelegate OnPacketReceived;

        /// <summary>
        /// Triggered when a connection to a given end point is lost.
        /// </summary>
        public event Action<IPEndPoint> OnConnectionLost;

        /// <summary>
        /// Adds a listener for ping events.
        /// </summary>
        public BackChannel(ILog logger) {
            this.logger = logger;
            AddPacketDelegate(PING, (msg, source) => Send(msg, source));

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()) {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            localAddresses.Add(addr.Address);
            }
        }

        /// <summary>
        /// The logger to use when writing to the logs.
        /// </summary>
        protected ILog Logger {
            get { return logger; }
            set { logger = value; }
        }

        /// <summary>
        /// True if the socket is bound.
        /// </summary>
        public bool Bound {
            get { return bound; }
        }

        /// <summary>
        /// The address that packets will be received on.
        /// "Not Bound" if not bound.
        /// </summary>
        public string Address {
            get { return bound ? ((IPEndPoint) socket.Client.LocalEndPoint).Address.ToString() : "Not Bound"; }
            set {
                if (bound)
                    throw new Exception("Unable to set address. Socket currently bound to " + Address);
                address = value;
            }
        }
        
        /// <summary>
        /// The port that packets will be received on.
        /// </summary>
        public int Port {
            get { return bound ? ((IPEndPoint) socket.Client.LocalEndPoint).Port : port; }
            set { port = bound ? Port : value; }
        }

        /// <summary>
        /// How many packets this slave has received.
        /// </summary>
        public int ReceivedPackets {
            get { return receivedPackets; }
        }

        /// <summary>
        /// Will be called whenever a send fails because a connection was forcibly closed by the remote host.
        /// </summary>
        protected abstract void ConnectionForciblyClosed();

        /// <summary>
        /// Turn a packet into an array of bytes. If necessary zerocde it. The returned array will be the correct length.
        /// </summary>
        /// <param name="packet">The packet to encode</param>
        protected byte[] GetBytes(OpenMetaverse.Packets.Packet packet) {
            byte[] bytes = packet.ToBytes();
            int length = bytes.Length;
            if (packet.Header.Zerocoded) {
                byte[] zerod = new byte[8192];
                length = Helpers.ZeroEncode(bytes, bytes.Length, zerod);
                bytes = zerod.Take(length).ToArray();
            }
            return bytes;
        }

        /// <summary>
        /// Send data to the specified end point.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="destination">The end point to send the data to.</param>
        public void Send(byte[] bytes, IPEndPoint destination) {
            if (!bound) {
                Logger.Info("Can't send packet. Socket has not yet been bound.");
                return;
            }
            try {
                socket.Send(bytes, bytes.Length, destination);
            } catch (SocketException e) {
                Logger.Info("Unable to send packet. " + e.Message);
            } catch (ObjectDisposedException e) {
                Logger.Info("Can't send packet through a closed socket.");
            }
        }

        /// <summary>
        /// Send a packet to the specified end point.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="destination">The end point to send the packet to.</param>
        public void Send(Packet packet, IPEndPoint destination) {
            Send(GetBytes(packet), destination);
        }

        /// <summary>
        /// Send a string to the specified end point.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        /// <param name="destination">The destination to send the packet to.</param>
        public void Send(string msg, IPEndPoint destination) {
            Send(Encoding.ASCII.GetBytes(msg), destination);
        }

        /// <summary>
        /// Add a delegate which controls how packets with certain data will be handled.
        /// </summary>
        /// <param name="identifier">If the data in a packet starts with this string the delegate will be called.</param>
        /// <param name="handler">The delegate that is called if the packet data starts with identifier.</param>
        protected void AddPacketDelegate(string identifier, MessageDelegate handler) {
            packetDelegates[identifier] = handler;
        }

        /// <summary>
        /// Disconnect the master server, unbinding all ports it had bound.
        /// </summary>
        protected void Unbind() {
            try {
                if (bound) {
                    bound = false;
                    socket.Close();
                    testConnectionSocket.Close();
                }
            } catch (Exception e) {
            }
        }

        /// <summary>
        /// Test which slave has been disconnected and remove it from the list of slaves.
        /// </summary>
        /// <param name="ep">The end point to check the connection with.</param>
        /// <param name="count">How many more attempts to make before stopping checking.</param>
        protected bool CheckConnection(IPEndPoint ep, int count) {
            if (count == 0) 
                return false;

            IPEndPoint testEP = new IPEndPoint(IPAddress.Any, 0);
            bool pingReceived = false;
            object pingLock = new object();
            if (testConnectionSocket.Client != null)
                try {
                    testConnectionSocket.BeginReceive(ar => {
                        try {
                            byte[] data = testConnectionSocket.EndReceive(ar, ref testEP);
                            pingReceived = Encoding.ASCII.GetString(data).Equals(InterProxyServer.PING);
                            lock (pingLock)
                                Monitor.PulseAll(pingLock);
                        } catch (ObjectDisposedException e) {
                        } catch (SocketException e) { }
                    }, ep);
                } catch (ObjectDisposedException e) {
                    Logger.Debug("BackChannel unable to test connection. TestConnectionSocket disposed.");
                }
            testConnectionSocket.Send(PING_B, PING_B.Length, ep);
            lock (pingLock)
                Monitor.Wait(pingLock, 1000);

            if (pingReceived)
                return true;
            else
                return CheckConnection(ep, --count);
        }

        /// <summary>
        /// Process incoming packets from slaves. Incoming packets are either connection requests or disconnect notifiers.
        /// </summary>
        private void PacketReceived(IAsyncResult ar) {
            if (socket == null)
                return;
            IPEndPoint source = new IPEndPoint(IPAddress.Any, 0);
            bool disposing = false;
            try {
                byte[] bytes = socket.EndReceive(ar, ref source);
                if (OnDataReceived != null)
                    OnDataReceived(bytes, bytes.Length, source);
                string msg = Encoding.ASCII.GetString(bytes);
                string key = packetDelegates.Keys.SingleOrDefault(str => msg.StartsWith(str));
                if (key != null)
                    packetDelegates[key](msg, source);
                else
                    ProcessPacket(bytes, source);
            } catch (ObjectDisposedException e) {
                disposing = true;
                return;
            } catch (SocketException e) {
                if (e.Message.Equals("An existing connection was forcibly closed by the remote host"))
                    ConnectionForciblyClosed();
                else
                    throw e;
            } finally {
                if (!disposing && socket.Client != null && socket.Client.IsBound)
                    socket.BeginReceive(PacketReceived, null);
            }
        }

        private void ProcessPacket(byte[] bytes, IPEndPoint source) {
            try {
                int length = bytes.Length - 1;
                Packet p = Packet.BuildPacket(bytes, ref length, zeroBuffer);
                Logger.Debug("Received " + p.Type + " packet from " + source + ".");
                try {
                    receivedPackets++;
                    if (OnPacketReceived != null)
                        OnPacketReceived(p, source);
                } catch (Exception e) {
                    Logger.Info("Problem in packet received delegate.", e);
                }
            } catch (Exception e) {
                Logger.Info("Problem unpacking packet from " + source + ".", e);
            }
        }

        protected bool Bind() {
            return Bind(address, Port);
        }

        /// <summary>
        /// Bind the UDP socket to a specific port.
        /// </summary>
        /// <param name="port">The port to bind the socket to.</param>
        protected bool Bind(string address, int port) {
            try {
                IPAddress ip = IPAddress.Any;
                foreach (IPAddress testIP in Dns.GetHostAddresses(address)) {
                    if (testIP.AddressFamily == AddressFamily.InterNetwork)
                        ip = testIP;

                }
                if (IPAddress.IsLoopback(ip))
                    foreach (IPAddress testIP in Dns.GetHostAddresses(Dns.GetHostName())) {
                        if (testIP.AddressFamily == AddressFamily.InterNetwork)
                            ip = testIP;

                }
                socket = new UdpClient(new IPEndPoint(ip, port));
                testConnectionSocket = new UdpClient(0);
                socket.BeginReceive(PacketReceived, null);
                bound = true;
                if (OnBound != null)
                    OnBound(socket.Client.LocalEndPoint, null);
                //Logger.Info("Bound to " + socket.Client.LocalEndPoint + ".");
                return true;
            } catch (SocketException e) {
                Logger.Info("Unable to bind channel. " + e.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// Callback for a matching message is received.
    /// </summary>
    /// 
    /// <param name="msg">The message received, as a string.</param>
    /// <param name="source">The source of the data.</param>
    public delegate void MessageDelegate(string msg, IPEndPoint source);


    /// <summary>
    /// Callback for when a datagram packet.
    /// </summary>
    /// <param name="data">The data contained in the packet.</param>
    /// <param name="length">The length of the data.</param>
    /// <param name="source">The source of the data.</param>
    public delegate void DataDelegate(byte[] data, int length, IPEndPoint source);
}