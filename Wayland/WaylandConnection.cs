using System;

namespace Wayland
{
    public abstract class WaylandConnection : IDisposable
    {
        public abstract void Marshal(uint id, ushort opcode, params object[] arguments);
        public abstract uint AllocateId();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
