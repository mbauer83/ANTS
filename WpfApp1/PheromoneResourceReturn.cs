using WpfApp1.utils.fn;

namespace WpfApp1;

public class PheromoneResourceReturn: ISimulationResource, SimulationObjectMixin
{
    public string Type { get; }
    public float X { get; }
    public float Y { get; }
    public float Amount { get; private set; }
    public float DecayRate { get; }
    public string Key { get; private set;  }
    
    public IOption<string> LockedByAgentId { get; set; } = new None<string>();
    
    public PheromoneResourceReturn(float x, float y, float amount, float decayRate)
    {
        this.Type = "pheromone-r";
        this.X = x;
        this.Y = y;
        this.Amount = amount;
        this.DecayRate = decayRate;
        this.Key = SimulationObjectMixin.KeyFor(Type, X, Y);
    }

    public (IOption<ISimulationResource>, IOption<ISimulationResource>) Split(float firstPartAmount)
    {
        ISimulationResource MapToPheromoneResource(SplittableAmount am)
        {
            return new PheromoneResource(X, Y, am.Amount, DecayRate);
        }
        var splittableAmount = new SplittableAmount(this.Amount);
        var split = splittableAmount.Split(firstPartAmount);
        var mappedItem1 = split.Item1.Map(MapToPheromoneResource) as IOption<ISimulationResource>;
        var mappedItem2 = split.Item2.Map(MapToPheromoneResource) as IOption<ISimulationResource>;
        return (mappedItem1, mappedItem2);
    }

    public ISimulationResource WithAmount(float amount)
    {
        return new PheromoneResourceReturn(X, Y, amount, DecayRate);
    }
    
    public ISimulationResource Decayed(float deltaTime, float decayRateModifier = 1f)
    {
        var newAmount = Amount * (1f - DecayRate * decayRateModifier * deltaTime);
        return new PheromoneResourceReturn(X, Y, newAmount, DecayRate);
    }
    
    public void Decay(float deltaTime, float decayRateModifier = 1f)
    {
        var newAmount = Amount * (1f - DecayRate * decayRateModifier * deltaTime);
        Amount = newAmount;
    }

}