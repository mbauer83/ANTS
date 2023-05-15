using System;
using System.Windows;
using ANTS.Definitions;
using ANTS.Utils.Functional;

namespace ANTS.Implementations;

public class PheromoneResource : SimulationResourceBase, ISimulationResource
{
    public PheromoneResource(Point pos, float amount, float decayRate)
    {
        Type = "pheromone";
        Position = pos;
        Amount = amount;
        DecayRate = decayRate;
        Key = SimulationObjectMixin.KeyFor(Type, X, Y);
    }

    public (IOption<ISimulationResource>, IOption<ISimulationResource>) Split(float firstPartAmount)
    {
        ISimulationResource MapToPheromoneResource(SplittableAmount am)
        {
            return new PheromoneResource(Position, am.Amount, DecayRate);
        }

        var splittableAmount = new SplittableAmount(Amount);
        var split = splittableAmount.Split(firstPartAmount);
        var mappedItem1 = split.Item1.Map(MapToPheromoneResource) as IOption<ISimulationResource>;
        var mappedItem2 = split.Item2.Map(MapToPheromoneResource) as IOption<ISimulationResource>;
        return (mappedItem1!, mappedItem2!);
    }

    public ISimulationResource WithAmount(float amount)
    {
        return new PheromoneResource(Position, amount, DecayRate);
    }

    public ISimulationResource Decayed(float deltaTime, float decayRateModifier = 1f)
    {
        var newAmount = Amount * (1f - DecayRate * decayRateModifier * deltaTime);
        return new PheromoneResource(Position, newAmount, DecayRate);
    }

}