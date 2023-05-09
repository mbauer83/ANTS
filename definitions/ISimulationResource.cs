using AntColonySimulation.utils.fn;

namespace AntColonySimulation.definitions;

public interface ISimulationResource
{
    public string Type { get;  }
    public float X { get;  }
    public float Y { get;  }
    public float Amount { get; set; }
    public string Key { get; }
    
    public float DecayRate { get; }
    
    public IOption<string> LockedByAgentId { get; set; }

    public (IOption<ISimulationResource>, IOption<ISimulationResource>) Split(float firstPartAmount);

    public ISimulationResource WithAmount(float amount);
    public ISimulationResource Decayed(float deltaTime, float decayRateModifier = 1f);
    public void Decay(float deltaTime, float decayRateModifier = 1f);
}