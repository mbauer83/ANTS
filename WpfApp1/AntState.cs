using System;
using System.Collections.Generic;

namespace WpfApp1;

public struct AntState: ISimulationAgentState 
{
    private BaseSimulationAgentState BaseState { get; }
    
    public float X => BaseState.X;
    public float Y => BaseState.Y;
    public float Orientation => BaseState.Orientation;
    public float Speed { get; }
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
        float sensoryFieldAngle = 140f,
        int sensoryFieldRadius = 100
    ) {
        this.BaseState = new BaseSimulationAgentState(x, y, sensoryFieldAngle, sensoryFieldRadius, orientation);
        this.Speed = speed;
        this.TotalFoodCarried = totalFoodCarried;
        this.SensoryFieldAngelRadHalved = sensoryFieldAngle * (float) Math.PI / 180f / 2f;
    }
    
    public AntState WithData(
        float? x = null, 
        float? y = null, 
        float? orientation = null, 
        float? speed = null, 
        float? totalFoodCarried = null,
        float? sensoryFieldAngle = null,
        int? sensoryFieldRadius = null
    ) {
        return new AntState(
            x ?? this.X, 
            y ?? this.Y, 
            orientation ?? this.Orientation, 
            speed ?? this.Speed, 
            totalFoodCarried ?? this.TotalFoodCarried,
            sensoryFieldAngle ?? this.SensoryFieldAngle,
            sensoryFieldRadius ?? this.SensoryFieldRadius
        );
    }

}