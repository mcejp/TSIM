using System.Collections.Generic;
using System.Numerics;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public interface IUnitDatabase
    {
        IEnumerable<Unit> EnumerateUnits();

        int GetNumUnits();
        ref Unit GetUnitByIndex(int unitIndex);
        void UpdateUnitByIndex(int unitIndex, Unit unit);

        byte[] SnapshotFullMake();
        void SnapshotFullRestore(byte[] snapshot);
    }
}
