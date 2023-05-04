namespace WpfApp1;

public interface ISimulationAgentState
{
    public float X { get; }
    public float Y { get; }
    public int SensoryFieldRadius { get;  }
    public float SensoryFieldAngle { get; }
    public float Orientation { get; }
}