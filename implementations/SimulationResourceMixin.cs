using AntColonySimulation.utils.fn;

namespace AntColonySimulation.implementations;

public interface SimulationResourceMixin: SimulationObjectMixin
{
    public IOption<int> LockedByAgentId { get; set; }
}