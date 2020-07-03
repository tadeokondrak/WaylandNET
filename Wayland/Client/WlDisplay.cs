namespace Wayland.Client
{
    public sealed class WlDisplay : WaylandProxy
    {
        internal WlDisplay(uint id, WaylandClientConnection connection)
            : base(id, connection)
        {
        }

        enum Error : uint
        {
            InvalidObject,
            InvalidMethod,
            NoMemory,
            Implementation,
        }

        enum Opcode : ushort
        {
            Sync,
            GetRegistry,
        }

        interface IListener
        {
            void Error(WlDisplay wl_display, WaylandProxy object_id, uint code, string message);
            void DeleteId(WlDisplay wl_display, WaylandProxy object_id);
        }

        WlCallback Sync()
        {
            uint id = Connection.AllocateId();
            Marshal((ushort)Opcode.Sync);
            return null;
        }

        WlRegistry GetRegistry()
        {
            Marshal((ushort)Opcode.GetRegistry);
            return null;
        }
    }

    public sealed class WlRegistry : WaylandProxy
    {
        internal WlRegistry(uint id, WaylandClientConnection connection)
            : base(id, connection)
        {
        }
    }

    public sealed class WlCallback : WaylandProxy
    {
        internal WlCallback(uint id, WaylandClientConnection connection)
            : base(id, connection)
        {
        }
    }
}
