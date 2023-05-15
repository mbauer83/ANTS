using System.Threading.Tasks;

namespace AntColonySimulation.Definitions;

public interface ISimulationAgent
{
    public string Id { get; }
    public ISimulationAgentState State { get; }
    public bool WithinSensoryField(float x1, float y1);
    public Task Act(ISimulationArena arena, float deltaTime);
}