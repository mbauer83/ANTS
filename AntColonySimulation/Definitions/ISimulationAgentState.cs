namespace AntColonySimulation.Definitions;

public interface ISimulationAgentState
{
    public float X { get; }
    public float Y { get; }
    public float Speed { get; }
    public int SensoryFieldRadius { get; }
    public float SensoryFieldAngle { get; }
    public float Orientation { get; }
}