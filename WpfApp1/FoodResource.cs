using System;
using System.ComponentModel;
using WpfApp1.utils.fn;

namespace WpfApp1;

public class FoodResource: ISimulationResource, SimulationObjectMixin
{
    public string Type { get; }
    public float X { get; }
    public float Y { get; }
    public float Amount { get; private set; }
    public float DecayRate { get; }
    public string Key { get; private set; }
    
    public IOption<string> LockedByAgentId { get; set; } = new None<string>();
    
    public FoodResource(float x, float y, float amount, float decayRate)
    {
        this.Type = "food";
        this.X = x;
        this.Y = y;
        this.Amount = amount;
        this.DecayRate = decayRate;
        this.Key = SimulationObjectMixin.KeyFor(Type, X, Y);
    }

    public (IOption<ISimulationResource>, IOption<ISimulationResource>) Split(float firstPartAmount)
    {
        ISimulationResource MapToFoodResource(SplittableAmount am)
        {
            return new FoodResource(X, Y, am.Amount, DecayRate);
        }
        var splittableAmount = new SplittableAmount(this.Amount);
        var split = splittableAmount.Split(firstPartAmount);
        var mappedItem1 = split.Item1.Map(MapToFoodResource) as IOption<ISimulationResource>;
        var mappedItem2 = split.Item2.Map(MapToFoodResource) as IOption<ISimulationResource>;
        return (mappedItem1, mappedItem2);
    }

    public ISimulationResource WithAmount(float amount)
    {
        return new FoodResource(X, Y, amount, DecayRate);
    }
    
    public ISimulationResource Decayed(float deltaTime, float decayRateModifier = 1f)
    {
        var newAmount = Amount * (1f - DecayRate * decayRateModifier * deltaTime);
        return new FoodResource(X, Y, newAmount, DecayRate);
    }

    public void Decay(float deltaTime, float decayRateModifier = 1f)
    {
        var newAmount = Amount * (1f - DecayRate * decayRateModifier * deltaTime);
        Amount = newAmount;
    }
    

}