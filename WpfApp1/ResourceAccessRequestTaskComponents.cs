namespace WpfApp1;

public record ResourceAccessRequestTaskComponents<T>(SimulationArena<T> Arena, ResourceAccessRequest<T> Request)
    where T: ISimulationAgentState;