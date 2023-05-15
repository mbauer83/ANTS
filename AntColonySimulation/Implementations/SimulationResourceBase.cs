using System;
using System.Windows;

namespace AntColonySimulation.Implementations;

public abstract class SimulationResourceBase : SimulationObjectMixin
{
    public string Type { get; protected set; }
    public float X => (float)Position.X;
    public float Y => (float)Position.Y;
    public float Amount { get; set; }
    public float DecayRate { get; set; }
    public string Key { get; protected set; }
    
    private Point _position;
    public Point Position
    {
        get => _position;
        set
        {
            _position = value;
            Key = SimulationObjectMixin.KeyFor(Type, X, Y);
        }
    }

    public virtual void Decay(float deltaTime, float decayRateModifier = 1f)
    {
        var evaporationRate = DecayRate * decayRateModifier;
        var newAmount = Amount * MathF.Pow(1 - evaporationRate, deltaTime);
        Amount = newAmount;
    }
}