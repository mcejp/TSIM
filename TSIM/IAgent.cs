namespace TSIM
{
    public interface IAgent
    {
        // Returns (unit index, acceleration for this dt)
        // TODO: This shall be redone in a better way!
        (int, float) Step(Simulation sim, double dt);
    }
}
