using System;

namespace Wayland
{
    public abstract class WaylandConnection : IDisposable
    {
        public abstract void Marshal(uint id, ushort opcode, params object[] arguments);
        public abstract uint AllocateId();
        public abstract void DeallocateId(uint id);
        public abstract WaylandObject this[uint id] { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
