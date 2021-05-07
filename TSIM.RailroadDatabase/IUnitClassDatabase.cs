using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public interface IUnitClassDatabase
    {
        UnitClass UnitClassByName(string name);
    }
}
