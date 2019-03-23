namespace TSIM.RailroadDatabase.Entity
{
    public class Unit
    {
        public int Id { get; set; }

        private Unit()
        {
        }

        public Unit(Model.Unit unit)
        {
        }

        public Model.Unit ToModel()
        {
            return new Model.Unit();
        }
    }
}
