using System.Collections;
using System.Collections.Generic;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public interface INetworkDatabase
    {
        IEnumerable<Segment> IterateSegments();
    }
}