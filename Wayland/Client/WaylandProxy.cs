namespace Wayland.Client
{
    public abstract class WaylandProxy : WaylandObject
    {
        uint id;
        WaylandClientConnection connection;

        public override uint Id => id;
        public override WaylandConnection Connection => connection;
        public WaylandClientConnection ClientConnection => connection;

        protected WaylandProxy(uint id, WaylandClientConnection connection)
        {
            this.id = id;
            this.connection = connection;
        }

        public override void Marshal(ushort opcode, object[] arguments)
        {
            Connection.Marshal(Id, opcode, arguments);
        }

        public abstract override void Handle(ushort opcode, object[] arguments);
        public abstract override WaylandType[] Arguments(ushort opcode);
    }
}
