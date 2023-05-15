using System.Threading.Tasks;

namespace ANTS.Definitions;

public interface ISimulationAgentBase
{
    public string Id { get; }
    public bool WithinSensoryField(float x1, float y1);
    public Task Act(ISimulationArena arena, float deltaTime);
    public ISimulationAgentState State { get; }
}