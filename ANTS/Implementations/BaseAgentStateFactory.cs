using System;
using System.Collections.Generic;
using ANTS.Definitions;

namespace ANTS.Implementations;

public class BaseAgentStateFactory : IAgentStateFactory<BaseSimulationAgentState>
{
    public BaseAgentStateFactory(int maxWidth, int maxHeight)
    {
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
    }

    private int MaxWidth { get; }
    private int MaxHeight { get; }

    public virtual BaseSimulationAgentState CreateState()
    {
        var rnd = new Random();
        return new BaseSimulationAgentState(
            rnd.Next(0, MaxWidth),
            rnd.Next(0, MaxHeight),
            130f,
            160f,
            80,
            (float)rnd.NextDouble() * 2 * (float)Math.PI
        );
    }

    public virtual List<BaseSimulationAgentState> CreateStates(int count)
    {
        var states = new List<BaseSimulationAgentState>();
        for (var i = 0; i < count; i++) states.Add(CreateState());
        return states;
    }
}