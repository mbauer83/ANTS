using AntColonySimulation.definitions;

namespace AntColonySimulation.implementations;

public record ResourceAccessRequestTaskComponents<T>(ISimulationArena<T> Arena, IResourceAccessRequest<T> Request)
    where T: ISimulationAgentState;