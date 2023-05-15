using AntColonySimulation.Utils.Functional;

namespace AntColonySimulation.Implementations;

public interface SimulationResourceMixin : SimulationObjectMixin
{
    public IOption<int> LockedByAgentId { get; set; }
}