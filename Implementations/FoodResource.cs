using AntColonySimulation.Definitions;
using AntColonySimulation.Utils.Functional;

namespace AntColonySimulation.Implementations;

public class FoodResource: ISimulationResource, SimulationObjectMixin
{
    public string Type { get; }
    public float X { get; }
    public float Y { get; }
    public float Amount { get; set; }
    public float DecayRate { get; }
    public string Key { get; private set; }
    
    public IOption<string> LockedByAgentId { get; set; } = new None<string>();
    
    public FoodResource(float x, float y, float amount, float decayRate)
    {
        Type = "food";
        X = x;
        Y = y;
        Amount = amount;
        DecayRate = decayRate;
        Key = SimulationObjectMixin.KeyFor(Type, X, Y);
    }

    public (IOption<ISimulationResource>, IOption<ISimulationResource>) Split(float firstPartAmount)
    {
        ISimulationResource MapToFoodResource(SplittableAmount am)
        {
            return new FoodResource(X, Y, am.Amount, DecayRate);
        }
        var splittableAmount = new SplittableAmount(Amount);
        var split = splittableAmount.Split(firstPartAmount);
        var mappedItem1 = split.Item1.Map(MapToFoodResource) as IOption<ISimulationResource>;
        var mappedItem2 = split.Item2.Map(MapToFoodResource) as IOption<ISimulationResource>;
        return (mappedItem1!, mappedItem2!);
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