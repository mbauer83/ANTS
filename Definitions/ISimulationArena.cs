using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AntColonySimulation.Utils.Functional;

namespace AntColonySimulation.Definitions;

public interface ISimulationArena
{
    int Width { get; }
    int Height { get; }
    Point Home { get; }
    ConcurrentDictionary<string, ISimulationResource> Resources { get; }
    
    List<(ISimulationResource, float, float)> ResourcesInSensoryField(
        ISimulationAgent agent,
        string resourceType,
        float exclusiveLowerLimit = 0f,
        float? exclusiveUpperLimit = null
    );

    // Not used in current simulation, but useful for other simulations and future extensions
    List<(ISimulationAgent, float)> AgentsInSensoryField(
        ISimulationAgent agent
    );

    bool WithinBounds(float x, float y);
    void AddPheromone(string type, Point pos, float amount, float decayRate);

    Task<IOption<float>> AttemptToTakeResourceAmount(string key, float maxAmount);

    
    // The following methods are only used on the concrete implementation
    // But are defined here for when we abstract the creation of the arena
    // in extending the solution
    Task RunGameLoop(int fps = 60);
    void OnMouseMove(object sender, MouseEventArgs e);
    void RaiseResourceDepletedEvent(string key);
    void TogglePause();
}