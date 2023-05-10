using AntColonySimulation.Definitions;

namespace AntColonySimulation.Implementations;

public record AgentUpdateContext<T1>(ISimulationArena<T1> Arena, float DeltaTime) where T1: ISimulationAgentState;