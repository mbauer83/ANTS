using AntColonySimulation.Utils.Functional;

namespace AntColonySimulation.Definitions;

public interface ISimulationAgent<T> where T: ISimulationAgentState
{
    
    public string Id { get; }
    public T State { get; }
    public bool WithinSensoryField(float x1, float y1);
    public void Act(ISimulationArena<T> arena, in float deltaTime);
    public void SolveResourceAccessRequest(IOption<ISimulationResource> maybeResource);

}