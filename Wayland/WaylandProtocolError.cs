using System;

namespace Wayland.Client
{
    class WaylandProtocolException : Exception
    {
        string message;

        public WaylandClientObject Object { get; private set; }
        public uint Code { get; private set; }
        public override string Message => message;

        internal WaylandProtocolException(WaylandClientObject @object, uint code, string message)
        {
            Object = @object;
            Code = code;
            this.message = message;
        }
    }
}
