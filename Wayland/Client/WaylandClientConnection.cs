using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Wayland;
using Wayland.Client.Protocol;

namespace Wayland.Client
{
    public sealed class WaylandClientConnection : WaylandConnection, IDisposable
    {
        public WlDisplay Display { get; private set; }

        static WaylandWireConnection ConnectToDisplay(string display = null)
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
            return new WaylandWireConnection(socket);
        }

        public WaylandClientConnection(string display = null)
            : base(ConnectToDisplay(display), new WaylandClientObjectMap())
        {
            uint id = ObjectMap.AllocateId();
            Display = new WlDisplay(id, 1, this);
            Display.Listener = new WlDisplayListener(this);
            ObjectMap[id] = Display;
        }

        class WlDisplayListener : WlDisplay.IListener
        {
            WaylandClientConnection connection;

            public WlDisplayListener(WaylandClientConnection connection)
            {
                this.connection = connection;
            }

            public void Error(WlDisplay wlDisplay, WaylandClientObject objectId, uint code, string message)
            {
                throw new WaylandProtocolException(objectId, code, message);
            }

            public void DeleteId(WlDisplay wlDisplay, uint id)
            {
                connection.DeallocateId(id);
            }
        }

    }
}
