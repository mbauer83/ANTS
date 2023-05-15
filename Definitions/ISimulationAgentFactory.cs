using System.Collections.Generic;

namespace AntColonySimulation.Definitions;

public interface ISimulationAgentFactory<T1, T2>
    where T1 : ISimulationAgent
    where T2 : ISimulationAgentState
{
    public T1 CreateAgent(IAgentStateFactory<T2> stateFactory);
    public List<T1> CreateAgents(IAgentStateFactory<T2> stateFactory, int count);
}