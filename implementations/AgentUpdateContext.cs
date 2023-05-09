using AntColonySimulation.definitions;

namespace AntColonySimulation.implementations;

public record AgentUpdateContext<T1>(ISimulationArena<T1> Arena, float DeltaTime) where T1: ISimulationAgentState;