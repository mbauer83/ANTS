using System.Threading.Tasks;
using AntColonySimulation.Utils.Functional;

namespace AntColonySimulation.Definitions;

public interface ISimulationAgent<T> where T: ISimulationAgentState
{
    
    public string Id { get; }
    public T State { get; }
    public bool WithinSensoryField(float x1, float y1);
    public Task Act(ISimulationArena<T> arena, float deltaTime);
    public void SolveResourceAccessRequest(IOption<ISimulationResource> maybeResource);

}