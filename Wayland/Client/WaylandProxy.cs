using Wayland;

namespace Wayland.Client
{
    public class WaylandProxy : WaylandObject
    {
        uint id;
        WaylandClientConnection connection;

        public override uint Id { get => id; }
        public override WaylandConnection Connection { get => connection; }

        protected WaylandProxy(uint id, WaylandClientConnection connection)
        {
            this.id = id;
            this.connection = connection;
        }

        public override void Marshal(ushort opcode, params object[] arguments)
        {
            Connection.Marshal(Id, opcode, arguments);
        }

        public override void Handle(ushort opcode, params object[] arguments)
        {
        }
    }
}
