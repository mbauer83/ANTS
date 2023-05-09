namespace AntColonySimulation.definitions;

public interface IResourceAccessRequest<T> where T : ISimulationAgentState
{
    string Type { get; }
    float X { get; }
    float Y { get; }
    float Amount { get; }
    ISimulationAgent<T> Agent { get; }
}