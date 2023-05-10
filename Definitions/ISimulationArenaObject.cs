using System;

namespace AntColonySimulation.Definitions;

public interface ISimulationArenaObject: ICloneable
{
    // In pixels from top left
    public int X { get; }
    public int Y { get; }
    // In radians single precision
    public float Orientation { get; }
}