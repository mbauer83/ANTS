using ANTS.Definitions;
using System.Windows;
using ANTS.Utils.Functional;

namespace ANTS.Implementations;

public class FoodResource : SimulationResourceBase, ISimulationResource
{
    public FoodResource(Point position, float amount, float decayRate)
    {
        Type = "food";
        Amount = amount;
        DecayRate = decayRate;
        Position = position;
        Key = SimulationObjectMixin.KeyFor(Type, X, Y);
    }

    public (IOption<ISimulationResource>, IOption<ISimulationResource>) Split(float firstPartAmount)
    {
        ISimulationResource MapToFoodResource(SplittableAmount am)
        {
            return new FoodResource(Position, am.Amount, DecayRate);
        }

        var splittableAmount = new SplittableAmount(Amount);
        var split = splittableAmount.Split(firstPartAmount);
        var mappedItem1 = split.Item1.Map(MapToFoodResource) as IOption<ISimulationResource>;
        var mappedItem2 = split.Item2.Map(MapToFoodResource) as IOption<ISimulationResource>;
        return (mappedItem1!, mappedItem2!);
    }

    public ISimulationResource WithAmount(float amount)
    {
        return new FoodResource(Position, amount, DecayRate);
    }

    public ISimulationResource Decayed(float deltaTime, float decayRateModifier = 1f)
    {
        var newAmount = Amount * (1f - DecayRate * decayRateModifier * deltaTime);
        return new FoodResource(Position, newAmount, DecayRate);
    }

}