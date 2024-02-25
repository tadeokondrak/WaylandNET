using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using WaylandNET.Client.Protocol;

namespace WaylandNET.Client
{
    public sealed class WaylandClientConnection : WaylandConnection
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
            Display.Error += (display, objectId, code, message) =>
            {
                throw new WaylandProtocolException(objectId, code, message);
            };
            Display.DeleteId += (display, id) =>
            {
                this.DeallocateId(id);
            };
            ObjectMap[id] = Display;
        }

        public void Roundtrip()
        {
            var callback = Display.Sync();
            var isDone = false;
            callback.Done += (callback, data) =>
            {
                isDone = true;
            };
            while (!isDone)
                Read();
        }
    }
}
