using System;
using System.Collections.Generic;

namespace Wayland
{
    public sealed class WaylandObjectMap<T> where T : WaylandObject
    {
        const uint ClientRangeBegin = 0x00000001;
        const uint ClientRangeEnd = 0xfeffffff;

        const uint ServerRangeBegin = 0xff000000;
        const uint ServerRangeEnd = 0xffffffff;

        List<T> clientObjects;
        List<int> clientFree;

        List<T> serverObjects;
        List<int> serverFree;

        public WaylandObjectMap()
        {
            clientObjects = new List<T>();
            clientFree = new List<int>();
            serverObjects = new List<T>();
            serverFree = new List<int>();
        }

        public uint AllocateClientId()
        {
            if (clientFree.Count != 0)
            {
                int idx = clientFree[clientFree.Count - 1];
                clientFree.RemoveAt(clientFree.Count - 1);
                return ClientRangeBegin + (uint)idx;
            }
            else
            {
                int idx = clientObjects.Count;
                clientObjects.Add(null);
                return ClientRangeBegin + (uint)idx;
            }
        }

        public uint AllocateServerId()
        {
            if (serverFree.Count != 0)
            {
                int idx = serverFree[serverFree.Count - 1];
                serverFree.RemoveAt(serverFree.Count - 1);
                return ServerRangeBegin + (uint)idx;
            }
            else
            {
                int idx = serverObjects.Count;
                serverObjects.Add(null);
                return ServerRangeBegin + (uint)idx;
            }
        }

        public void DeallocateId(uint id)
        {
        }

        public T this[uint id]
        {
            get {
                if (id >= ClientRangeBegin && id <= ClientRangeEnd)
                    return clientObjects[(int)(id - ClientRangeBegin)];
                else if (id >= ServerRangeBegin && id <= ServerRangeEnd)
                    return serverObjects[(int)(id - ServerRangeBegin)];
                else
                    throw new IndexOutOfRangeException();
            }
            set {
                if (id >= ClientRangeBegin && id <= ClientRangeEnd)
                    clientObjects[(int)(id - ClientRangeBegin)] = value;
                else if (id >= ServerRangeBegin && id <= ServerRangeEnd)
                    serverObjects[(int)(id - ServerRangeBegin)] = value;
                else
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
