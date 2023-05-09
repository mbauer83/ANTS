using AntColonySimulation.definitions;

namespace AntColonySimulation.implementations;

public record AgentTaskComponents<T1>(ISimulationAgent<T1> Agent, AgentUpdateContext<T1> UpdateContext) where T1: ISimulationAgentState;