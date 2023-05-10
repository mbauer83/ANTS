using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AntColonySimulation.Utils.Functional;

namespace AntColonySimulation.Definitions;

public interface ISimulationArena<T> where T : ISimulationAgentState
{
    int Width { get; }
    int Height { get; }
    Point Home { get; }
    ConcurrentDictionary<string, ISimulationResource> Resources { get; }
    
    void TogglePause();

    List<(ISimulationResource, float, float)> ResourcesInSensoryField(
        ISimulationAgent<T> agent,
        string resourceType,
        float exclusiveLowerLimit = 0f,
        float? exclusiveUpperLimit = null
    );

    List<(ISimulationAgent<T>, float)> AgentsInSensoryField(
        ISimulationAgent<T> agent
    );

    bool WithinBounds(float x, float y);
    void AddPheromone(string type, Point pos, float amount, float decayRate);

    Task<IOption<float>> AttemptToTakeResourceAmount(string key, float maxAmount);
    Task RunGameLoop(int fps = 60);
    void OnMouseMove(object sender, MouseEventArgs e);
    void RaiseResourceDepletedEvent(string key);
}