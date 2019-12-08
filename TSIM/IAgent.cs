namespace TSIM
{
    public interface IAgent
    {
        // TODO: This shall be redone in a better way!
        (int, float, float) Step(Simulation sim, double dt);
    }
}
