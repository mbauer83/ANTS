using System;
using System.Threading.Tasks;
using WpfApp1.utils.fn;

namespace WpfApp1;

public interface ISimulationAgent<T> where T: ISimulationAgentState
{
    
    public string Id { get; }
    public T State { get; }
    public bool WithinSensoryField(float x1, float y1);
    public void Act(SimulationArena<T> arena, float deltaTime);
    public void SolveResourceAccessRequest(IOption<ISimulationResource> maybeResource);

}