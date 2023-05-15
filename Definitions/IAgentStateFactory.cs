using System.Collections.Generic;

namespace AntColonySimulation.Definitions;

public interface IAgentStateFactory<T> where T : ISimulationAgentState
{
    public T CreateState();
    public List<T> CreateStates(int count);
}