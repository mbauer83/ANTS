using ANTS.Definitions;

namespace ANTS.Implementations;

public struct BaseSimulationAgentState : ISimulationAgentState
{
    public float X { get; }
    public float Y { get; }
    public float Speed { get; }
    public float Orientation { get; }
    public int SensoryFieldRadius { get; }
    public float SensoryFieldAngle { get; }

    public BaseSimulationAgentState(
        float x,
        float y,
        float speed,
        float sensoryFieldAngle,
        int sensoryFieldRadius,
        float orientation = 0f
    )
    {
        X = x;
        Y = y;
        Speed = speed;
        SensoryFieldAngle = sensoryFieldAngle;
        SensoryFieldRadius = sensoryFieldRadius;
        Orientation = orientation;
    }
}