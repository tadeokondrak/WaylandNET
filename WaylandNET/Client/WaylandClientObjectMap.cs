using System;
using System.Collections.Generic;

namespace WaylandNET.Client
{
    public class WaylandClientObjectMap : WaylandObjectMap
    {
        List<int> free;

        public WaylandClientObjectMap()
        {
            free = new List<int>();
        }

        public override uint AllocateId()
        {
            if (free.Count != 0)
            {
                int idx = free[free.Count - 1];
                free.RemoveAt(free.Count - 1);
                return ClientRangeBegin + (uint)idx;
            }
            else
            {
                int idx = clientObjects.Count;
                clientObjects.Add(null);
                return ClientRangeBegin + (uint)idx;
            }
        }

        public override void DeallocateId(uint id)
        {
            base[id] = null;
            if (ClientRangeBegin <= id && id <= ClientRangeEnd)
                free.Add((int)(id - ClientRangeBegin));
            else if (ServerRangeBegin <= id && id <= ServerRangeEnd)
                throw new IndexOutOfRangeException("Cannot deallocate server-range ID");
            else
                throw new IndexOutOfRangeException("Cannot deallocate null ID");
        }
    }
}
