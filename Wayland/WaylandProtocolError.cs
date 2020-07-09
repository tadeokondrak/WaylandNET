using System;

namespace Wayland.Client
{
    class WaylandProtocolException : Exception
    {
        string message;

        public WaylandProxy Object { get; private set; }
        public uint Code { get; private set; }
        public override string Message => message;

        internal WaylandProtocolException(WaylandProxy @object, uint code, string message)
        {
            Object = @object;
            Code = code;
            this.message = message;
        }
    }
}
