using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Wayland;
using Wayland.Client.Protocol;

namespace Wayland.Client
{
    public sealed class WaylandClientConnection : WaylandConnection, IDisposable
    {
        WaylandWireConnection wireConnection;
        WaylandObjectMap<WaylandProxy> objectMap;

        public WlDisplay Display { get; private set; }

        public WaylandClientConnection(string display = null)
        {
            display ??= Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            display ??= "wayland-0";

            string path;
            if (display.StartsWith('/'))
            {
                path = display;
            }
            else
            {
                string xdgRuntimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
                if (xdgRuntimeDir == null)
                    throw new Exception("XDG_RUNTIME_DIR missing from environment");
                path = Path.Join(xdgRuntimeDir, display);
            }

            Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            EndPoint endpoint = new UnixDomainSocketEndPoint(path);
            socket.Connect(endpoint);

            wireConnection = new WaylandWireConnection(socket);
            objectMap = new WaylandObjectMap<WaylandProxy>();

            uint id = objectMap.AllocateClientId();
            Display = new WlDisplay(id, 1, this);
            Display.Listener = new WlDisplayListener(this);
            objectMap[id] = Display;
        }

        public void Read()
        {
            while (true)
            {
                WaylandMessageHeader message = wireConnection.ReadMessageHeader();
                WaylandProxy proxy = objectMap[message.id];
                WaylandType[] argumentTypes = proxy.Arguments(message.opcode);
                List<Object> arguments = new List<Object>();
                foreach (WaylandType type in argumentTypes)
                {
                    switch (type)
                    {
                        case WaylandType.Int:
                            arguments.Add(wireConnection.ReadInt32());
                            break;
                        case WaylandType.UInt:
                            arguments.Add(wireConnection.ReadUInt32());
                            break;
                        case WaylandType.Fixed:
                            arguments.Add(wireConnection.ReadDouble());
                            break;
                        case WaylandType.Object:
                            arguments.Add(wireConnection.ReadUInt32());
                            break;
                        case WaylandType.NewId:
                            arguments.Add(wireConnection.ReadUInt32());
                            break;
                        case WaylandType.String:
                            arguments.Add(wireConnection.ReadString());
                            break;
                        case WaylandType.Array:
                            arguments.Add(wireConnection.ReadBytes());
                            break;
                        case WaylandType.Handle:
                            arguments.Add(wireConnection.ReadHandle());
                            break;
                    }
                }
                proxy.Handle(message.opcode, arguments.ToArray());
            }
        }

        public void Flush()
        {
            wireConnection.Flush();
        }

        public void Dispatch()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                wireConnection.Dispose();
        }

        public override void Marshal(uint id, ushort opcode, params object[] arguments)
        {
            ushort size = 8;
            foreach (object argument in arguments)
            {
                switch (argument)
                {
                    case int i:
                    case uint u:
                    case double d:
                        size += 4;
                        break;
                    case string s:
                        size += 4;
                        if (s != null)
                            size += (ushort)((Encoding.UTF8.GetByteCount(s) + 4) / 4 * 4);
                        break;
                    case byte[] a:
                        size += 4;
                        if (a != null)
                            size += (ushort)((a.Length + 3) / 4 * 4);
                        break;
                    case IntPtr h:
                        break;
                }
            }
            wireConnection.Write(id);
            wireConnection.Write(((uint)size << 16) | (uint)opcode);
            foreach (object argument in arguments)
            {
                switch (argument)
                {
                    case int i:
                        wireConnection.Write(i);
                        break;
                    case uint u:
                        wireConnection.Write(u);
                        break;
                    case double d:
                        wireConnection.Write(d);
                        break;
                    case string s:
                        wireConnection.Write(s);
                        break;
                    case byte[] a:
                        wireConnection.Write(a);
                        break;
                    case IntPtr h:
                        wireConnection.Write(h);
                        break;
                }
            }
        }

        public override uint AllocateId()
        {
            return objectMap.AllocateClientId();
        }

        public override WaylandObject this[uint id]
        {
            get => objectMap[id];
            set => objectMap[id] = (WaylandProxy)value;
        }

        class WlDisplayListener : WlDisplay.IListener
        {
            WaylandClientConnection connection;

            public WlDisplayListener(WaylandClientConnection connection)
            {
                this.connection = connection;
            }

            public void Error(WlDisplay wlDisplay, WaylandProxy objectId, uint code, string message)
            {
                throw new WaylandProtocolException(objectId, code, message);
            }

            public void DeleteId(WlDisplay wlDisplay, uint id)
            {
                connection.objectMap.DeallocateId(id);
            }
        }

    }
}
