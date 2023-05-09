using AntColonySimulation.utils.fn;

namespace AntColonySimulation.definitions;

public interface ISimulationAgent<T> where T: ISimulationAgentState
{
    
    public string Id { get; }
    public T State { get; }
    public bool WithinSensoryField(float x1, float y1);
    public void Act(ISimulationArena<T> arena, in float deltaTime);
    public void SolveResourceAccessRequest(IOption<ISimulationResource> maybeResource);

}