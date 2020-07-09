namespace Wayland.Client
{
    public abstract class WaylandProxy : WaylandObject
    {
        public WaylandClientConnection ClientConnection { get; private set; }

        protected WaylandProxy(string @interface, uint id, uint version,
            WaylandClientConnection connection) : base(@interface, id, version, connection)
        {
            ClientConnection = connection;
        }

        public abstract override void Handle(ushort opcode, object[] arguments);
        public abstract override WaylandType[] Arguments(ushort opcode);
    }
}
