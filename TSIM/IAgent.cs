namespace TSIM
{
    public interface IAgent
    {
        // This shall be redone in a better way
        (int, float) Step(Simulation sim, double dt);
    }
}
