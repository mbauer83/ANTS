namespace WpfApp1;

public record AgentUpdateContext<T1>(SimulationArena<T1> Arena, float DeltaTime) where T1: ISimulationAgentState;