using System;
using System.Text;
using System.Collections.Generic;

namespace WaylandNET
{
    public abstract class WaylandConnection : IDisposable
    {
        public WaylandObject this[uint id]
        {
            get => ObjectMap[id];
            set => ObjectMap[id] = value;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public uint AllocateId()
        {
            return ObjectMap.AllocateId();
        }

        public void DeallocateId(uint id)
        {
            ObjectMap.DeallocateId(id);
        }

        public void Read()
        {
            WaylandMessageHeader message = WireConnection.ReadMessageHeader();
            WaylandObject @object = ObjectMap[message.id];
            WaylandType[] argumentTypes = @object.Arguments(message.opcode);
            List<object> arguments = new List<object>();
            foreach (WaylandType type in argumentTypes)
            {
                switch (type)
                {
                    case WaylandType.Int:
                        arguments.Add(WireConnection.ReadInt32());
                        break;
                    case WaylandType.UInt:
                        arguments.Add(WireConnection.ReadUInt32());
                        break;
                    case WaylandType.Fixed:
                        arguments.Add(WireConnection.ReadDouble());
                        break;
                    case WaylandType.Object:
                        arguments.Add(WireConnection.ReadUInt32());
                        break;
                    case WaylandType.NewId:
                        arguments.Add(WireConnection.ReadUInt32());
                        break;
                    case WaylandType.String:
                        arguments.Add(WireConnection.ReadString());
                        break;
                    case WaylandType.Array:
                        arguments.Add(WireConnection.ReadBytes());
                        break;
                    case WaylandType.Handle:
                        arguments.Add(WireConnection.ReadHandle());
                        break;
                }
            }
            @object.Handle(message.opcode, arguments.ToArray());
        }

        public void Marshal(uint id, ushort opcode, params object[] arguments)
        {
            ushort size = 8;
            foreach (object argument in arguments)
            {
                switch (argument)
                {
                    case int i:
                    case uint u:
                    case double d:
                        size += 4;
                        break;
                    case string s:
                        size += 4;
                        if (s != null)
                            size += (ushort)((Encoding.UTF8.GetByteCount(s) + 4) / 4 * 4);
                        break;
                    case byte[] a:
                        size += 4;
                        if (a != null)
                            size += (ushort)((a.Length + 3) / 4 * 4);
                        break;
                    case IntPtr h:
                        break;
                }
            }
            WireConnection.Write(id);
            WireConnection.Write(((uint)size << 16) | (uint)opcode);
            foreach (object argument in arguments)
            {
                switch (argument)
                {
                    case int i:
                        WireConnection.Write(i);
                        break;
                    case uint u:
                        WireConnection.Write(u);
                        break;
                    case double d:
                        WireConnection.Write(d);
                        break;
                    case string s:
                        WireConnection.Write(s);
                        break;
                    case byte[] a:
                        WireConnection.Write(a);
                        break;
                    case IntPtr h:
                        WireConnection.Write(h);
                        break;
                }
            }
        }

        protected WaylandWireConnection WireConnection { get; private set; }
        protected WaylandObjectMap ObjectMap { get; private set; }

        protected WaylandConnection(WaylandWireConnection wireConnection, WaylandObjectMap objectMap)
        {
            WireConnection = wireConnection;
            ObjectMap = objectMap;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                WireConnection.Dispose();
        }
    }
}
