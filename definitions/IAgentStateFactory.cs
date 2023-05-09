using System.Collections.Generic;

namespace AntColonySimulation.definitions;

public interface IAgentStateFactory<T> where T: ISimulationAgentState
{
    public T CreateState();
    public List<T> CreateStates(int count);
}