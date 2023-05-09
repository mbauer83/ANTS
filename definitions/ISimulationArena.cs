using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AntColonySimulation.definitions;

public interface ISimulationArena<T> where T : ISimulationAgentState
{
    int Width { get; }
    int Height { get; }
    Point Home { get; }
    void Stop();

    List<(ISimulationResource, float, float)> ResourcesInSensoryField(
        ISimulationAgent<T> agent,
        string resourceType,
        float exclusiveLowerLimit = 0f,
        float? exclusiveUpperLimit = null
    );

    List<(ISimulationAgent<T>, float)> AgentsInSensoryField(
        ISimulationAgent<T> agent
    );

    void AddResourceAccessRequest(IResourceAccessRequest<T> req);
    bool WithinBounds(float x, float y);
    void AddPheromone(string type, Point pos, float amount, float decayRate);
    Task RunGameLoop(int fps = 60);
    void OnMouseMove(object sender, MouseEventArgs e);
    void ProcessResourceAccessRequest(IResourceAccessRequest<T> req);
}