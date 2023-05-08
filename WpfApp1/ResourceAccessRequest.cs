namespace WpfApp1;

public struct ResourceAccessRequest<T> where T: ISimulationAgentState
{
    
    public string Type { get; }
    public float X { get; }
    public float Y { get; }
    public float Amount { get; }
    public ISimulationAgent<T> Agent { get; }
    
    public ResourceAccessRequest(string type, float x, float y, float amount, ISimulationAgent<T> agent)
    {
        Type = type;
        X = x;
        Y = y;
        Amount = amount;
        Agent = agent;
    }
}