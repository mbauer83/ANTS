using System;
using ANTS.Definitions;

namespace ANTS.Implementations;

public class AntState : ISimulationAgentState
{
    private BaseSimulationAgentState BaseState { get; }

    public float X => BaseState.X;
    public float Y => BaseState.Y;
    public float Orientation => BaseState.Orientation;
    public float Speed => BaseState.Speed;
    public float SensoryFieldAngle => BaseState.SensoryFieldAngle;

    public int SensoryFieldRadius => BaseState.SensoryFieldRadius;

    public float TotalFoodCarried { get; }

    public float SensoryFieldAngelRadHalved { get; }


    public AntState(
        float x,
        float y,
        float orientation,
        float speed,
        float totalFoodCarried,
        float sensoryFieldAngle = 120f,
        int sensoryFieldRadius = 120
    )
    {
        BaseState = new BaseSimulationAgentState(x, y, speed, sensoryFieldAngle, sensoryFieldRadius, orientation);
        TotalFoodCarried = totalFoodCarried;
        SensoryFieldAngelRadHalved = sensoryFieldAngle * (float)Math.PI / 180f / 2f;
    }

    public AntState WithData(
        float? x = null,
        float? y = null,
        float? orientation = null,
        float? speed = null,
        float? totalFoodCarried = null,
        float? sensoryFieldAngle = null,
        int? sensoryFieldRadius = null
    )
    {
        return new AntState(
            x ?? X,
            y ?? Y,
            orientation ?? Orientation,
            speed ?? Speed,
            totalFoodCarried ?? TotalFoodCarried,
            sensoryFieldAngle ?? SensoryFieldAngle,
            sensoryFieldRadius ?? SensoryFieldRadius
        );
    }
}