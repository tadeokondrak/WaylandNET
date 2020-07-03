using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Wayland;

namespace Wayland.Client
{
    public sealed class WaylandClientConnection : WaylandConnection, IDisposable
    {
        WaylandWireConnection wireConnection;
        WaylandObjectMap<WaylandProxy> objectMap;

        public WlDisplay Display => (WlDisplay)objectMap[0];

        public WaylandClientConnection(string display = null)
        {
            display ??= Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            if (display == null)
                display = "wayland-0";

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
            objectMap[id] = new WlDisplay(id, this);
        }

        public void Flush()
        {
            wireConnection.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                wireConnection.Dispose();
            }
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
                    case WaylandObject o:
                        size += 4;
                        break;
                    case string s:
                        size += 4;
                        if (s != null)
                            size += (ushort)(Encoding.UTF8.GetByteCount(s) + 3 / 4 * 4);
                        break;
                    case byte[] a:
                        size += 4;
                        if (a != null)
                            size += (ushort)(a.Length + 3 / 4 * 4);
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
                    case WaylandObject o:
                        if (o == null)
                            wireConnection.Write((uint)0);
                        else
                            wireConnection.Write(o.Id);
                        break;
                    case string s:
                        wireConnection.Write(s);
                        break;
                    case byte[] a:
                        wireConnection.Write(a);
                        break;
                }
            }
        }

        public override uint AllocateId()
        {
            return objectMap.AllocateClientId();
        }
    }
}
