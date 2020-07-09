namespace Wayland
{
    public abstract class WaylandObject
    {
        public string Interface { get; private set; }
        public uint Id { get; private set; }
        public uint Version { get; private set; }
        public WaylandConnection Connection { get; private set; }

        public abstract void Marshal(ushort opcode, params object[] arguments);
        public abstract void Handle(ushort opcode, params object[] arguments);
        public abstract WaylandType[] Arguments(ushort opcode);

        protected WaylandObject(string @interface, uint id, uint version, WaylandConnection connection)
        {
            Interface = @interface;
            Id = id;
            Version = version;
            Connection = connection;
        }
    }
}
