using System;
using System.Collections.Generic;

namespace WpfApp1;

public class BaseAgentStateFactory: IAgentStateFactory<BaseSimulationAgentState>
{
    private int MaxWidth { get; }
    private int MaxHeight { get; }
    
    public BaseAgentStateFactory(int maxWidth, int maxHeight)
    {
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
    }
    
    public virtual BaseSimulationAgentState CreateState()
    {
        var rnd = new Random();
        return new BaseSimulationAgentState(
            rnd.Next(0, MaxWidth),
            rnd.Next(0, MaxHeight),
            160f,
            80,
            (float) rnd.NextDouble() * 2 * (float) Math.PI
        );
    }
    
    public virtual List<BaseSimulationAgentState> CreateStates(int count)
    {
        var states = new List<BaseSimulationAgentState>();
        for (var i = 0; i < count; i++)
        {
            states.Add(CreateState());
        }
        return states;
    }
}