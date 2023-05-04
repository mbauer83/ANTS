using System.Collections.Generic;

namespace WpfApp1;

public interface IAgentStateFactory<T> where T: ISimulationAgentState
{
    public T CreateState();
    public List<T> CreateStates(int count);
}