using System;

namespace Wayland
{
    public abstract class WaylandObject
    {
        public abstract uint Id { get; }
        public abstract WaylandConnection Connection { get; }

        public abstract void Marshal(ushort opcode, params object[] arguments);
        public abstract void Handle(ushort opcode, params object[] arguments);
    }
}
