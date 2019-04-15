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
        /**
         * @deprecated this is idiot. dont use ever
         */
        void SetUnitSpeed(int id, float speed);
        void UpdateUnitByIndex(int unitIndex, Unit unit);
    }
}
