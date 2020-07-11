using System;
using System.Collections.Generic;

namespace WaylandNET
{
    public abstract class WaylandObjectMap
    {
        protected const uint ClientRangeBegin = 0x00000001;
        protected const uint ClientRangeEnd = 0xfeffffff;

        protected const uint ServerRangeBegin = 0xff000000;
        protected const uint ServerRangeEnd = 0xffffffff;

        protected List<WaylandObject> clientObjects;
        protected List<WaylandObject> serverObjects;

        protected WaylandObjectMap()
        {
            clientObjects = new List<WaylandObject>();
            serverObjects = new List<WaylandObject>();
        }

        public abstract uint AllocateId();
        public abstract void DeallocateId(uint id);

        public WaylandObject this[uint id]
        {
            get {
                if (id >= ClientRangeBegin && id <= ClientRangeEnd)
                    return clientObjects[(int)(id - ClientRangeBegin)];
                else if (id >= ServerRangeBegin && id <= ServerRangeEnd)
                    return serverObjects[(int)(id - ServerRangeBegin)];
                else
                    return null;
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
