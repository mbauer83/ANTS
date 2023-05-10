using System;
using System.Windows;
using AntColonySimulation.definitions;
using AntColonySimulation.utils.fn;

namespace AntColonySimulation.implementations;

public class PheromoneResourceReturn: ISimulationResource, SimulationObjectMixin
{
    public string Type { get; }
    public float X { get; private set; }
    public float Y { get; private set;  }

    private Point _position;
    public Point Position
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
            X = (float)value.X;
            Y = (float)value.Y;
            Key = SimulationObjectMixin.KeyFor(Type, X, Y);
        }
    }

    public float Amount { get; set; }
    public float DecayRate { get; set; }
    public string Key { get; private set;  }
    
    public IOption<string> LockedByAgentId { get; set; } = new None<string>();
    
    public PheromoneResourceReturn(Point pos, float amount, float decayRate)
    {
        Type = "pheromone-r";
        Position = pos;
        X = (float)pos.X;
        Y = (float)pos.Y;
        Amount = amount;
        DecayRate = decayRate;
        Key = SimulationObjectMixin.KeyFor(Type, X, Y);
    }

    public (IOption<ISimulationResource>, IOption<ISimulationResource>) Split(float firstPartAmount)
    {
        ISimulationResource MapToPheromoneResource(SplittableAmount am)
        {
            return new PheromoneResourceReturn(Position, am.Amount, DecayRate);
        }
        var splittableAmount = new SplittableAmount(Amount);
        var split = splittableAmount.Split(firstPartAmount);
        var mappedItem1 = split.Item1.Map(MapToPheromoneResource) as IOption<ISimulationResource>;
        var mappedItem2 = split.Item2.Map(MapToPheromoneResource) as IOption<ISimulationResource>;
        return (mappedItem1!, mappedItem2!);
    }

    public ISimulationResource WithAmount(float amount)
    {
        return new PheromoneResourceReturn(Position, amount, DecayRate);
    }
    
    public ISimulationResource Decayed(float deltaTime, float decayRateModifier = 1f)
    {
        var newAmount = Amount * (1f - DecayRate * decayRateModifier * deltaTime);
        return new PheromoneResourceReturn(Position, newAmount, DecayRate);
    }
    
    public void Decay(float deltaTime, float decayRateModifier = 1f)
    {
        var evaporationRate = DecayRate * decayRateModifier * MathF.Pow(1- Amount, 2);
        var newAmount = Amount * MathF.Pow(1 - evaporationRate, deltaTime);
        //var newAmount = Amount * (1f - DecayRate * decayRateModifier * deltaTime);
        Amount = newAmount;
    }

}