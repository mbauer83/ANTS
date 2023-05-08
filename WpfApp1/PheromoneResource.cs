using System;
using WpfApp1.utils.fn;

namespace WpfApp1;

public class PheromoneResource: ISimulationResource, SimulationObjectMixin
{
    public string Type { get; }
    public float X { get; }
    public float Y { get; }
    public float Amount { get; private set; }
    public float DecayRate { get; }
    public string Key { get; private set;  }
    
    public IOption<string> LockedByAgentId { get; set; } = new None<string>();
    
    public PheromoneResource(float x, float y, float amount, float decayRate)
    {
        Type = "pheromone";
        X = x;
        Y = y;
        Amount = amount;
        DecayRate = decayRate;
        Key = SimulationObjectMixin.KeyFor(Type, X, Y);
    }

    public (IOption<ISimulationResource>, IOption<ISimulationResource>) Split(float firstPartAmount)
    {
        ISimulationResource MapToPheromoneResource(SplittableAmount am)
        {
            return new PheromoneResource(X, Y, am.Amount, DecayRate);
        }
        var splittableAmount = new SplittableAmount(Amount);
        var split = splittableAmount.Split(firstPartAmount);
        var mappedItem1 = split.Item1.Map(MapToPheromoneResource) as IOption<ISimulationResource>;
        var mappedItem2 = split.Item2.Map(MapToPheromoneResource) as IOption<ISimulationResource>;
        return (mappedItem1, mappedItem2);
    }

    public ISimulationResource WithAmount(float amount)
    {
        return new PheromoneResource(X, Y, amount, DecayRate);
    }
    
    public ISimulationResource Decayed(float deltaTime, float decayRateModifier = 1f)
    {
        var newAmount = Amount * (1f - DecayRate * decayRateModifier * deltaTime);
        return new PheromoneResource(X, Y, newAmount, DecayRate);
    }
    
    public void Decay(float deltaTime, float decayRateModifier = 1f)
    {
        var evaporationRate = DecayRate * decayRateModifier * MathF.Pow(1- Amount, 2);
        var newAmount = Amount * MathF.Pow(1 - evaporationRate, deltaTime);
        //var newAmount = Amount * (1f - DecayRate * decayRateModifier * deltaTime);
        Amount = newAmount;
    }

}