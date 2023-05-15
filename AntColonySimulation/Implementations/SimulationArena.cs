using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AntColonySimulation.Definitions;
using AntColonySimulation.Utils.Functional;
using AntColonySimulation.Utils.Geometry;

namespace AntColonySimulation.Implementations;

public class SimulationArena : ISimulationArena
{
    private readonly List<ISimulationAgent> _agents;
    private readonly SimulationCanvas _canvas;
    private readonly IOption<float> _noneFloat = new None<float>();
    private readonly PheromoneResourcePool _pheromoneResourcePool;
    private readonly PheromoneResourceReturnPool _pheromoneResourceReturnPool;

    private bool _run;
    public readonly EventHandler<ResourceDepletedEventArgs> ResourceDepleted;

    public SimulationArena(
        int width,
        int height,
        ConcurrentDictionary<string, ISimulationResource> resources,
        IEnumerable<ISimulationAgent> agents,
        SimulationCanvas canvas,
        PheromoneResourcePool pheromoneResourcePool,
        PheromoneResourceReturnPool pheromoneResourceReturnPool
    )
    {
        Width = width;
        Height = height;
        Resources = resources;
        _agents = agents.ToList();
        _canvas = canvas;
        Home = new Point(width / 4f, height / 2f);
        ResourceDepleted += _canvas.OnResourceDepleted;
        _pheromoneResourcePool = pheromoneResourcePool;
        _pheromoneResourceReturnPool = pheromoneResourceReturnPool;
    }

    public int Width { get; }
    public int Height { get; }
    public Point Home { get; }
    public ConcurrentDictionary<string, ISimulationResource> Resources { get; }

    public void TogglePause()
    {
        _run = !_run;
    }

    public List<(ISimulationResource, float, float)> ResourcesInSensoryField(
        ISimulationAgent agent,
        string resourceType,
        float exclusiveLowerLimit = 0f,
        float? exclusiveUpperLimit = null
    )
    {
        var list = (from res in Resources
            where res.Value.Type == resourceType && res.Value.Amount > exclusiveLowerLimit &&
                  (exclusiveUpperLimit == null || res.Value.Amount < exclusiveUpperLimit) &&
                  agent.WithinSensoryField(res.Value.X, res.Value.Y)
            let distance = Geometry2D.EuclideanDistance(agent.State.X, agent.State.Y, res.Value.X, res.Value.Y)
            let relativeOrientation = Geometry2D.AngleBetween(agent.State.X, agent.State.Y, res.Value.X, res.Value.Y)
            select (res.Value, distance, relativeOrientation)).ToList();
        list.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
        return list;
    }

    public List<(ISimulationAgent, float)> AgentsInSensoryField(ISimulationAgent agent)
    {
        
        var list = (from otherAgent in _agents
            where 
                  agent.WithinSensoryField(otherAgent.State.X, otherAgent.State.Y) &&
                  agent.Id != otherAgent.Id
            let distance = Geometry2D.EuclideanDistance(agent.State.X, agent.State.Y, otherAgent.State.X, otherAgent.State.Y)
            select (otherAgent, distance)).ToList();
        list.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
        return list;
    }

    public bool WithinBounds(float x, float y)
    {
        const float buffer = 5f;
        return x >= buffer && x <= Width - buffer && y >= buffer && y <= Height - buffer;
    }

    public async Task RunGameLoop(int fps = 30)
    {
        _run = true;
        var stopwatch = new Stopwatch();
        var fixedTimeStep = 1f / fps;
        var i = 0;
        while (true)
            if (_run)
            {
                stopwatch.Reset();
                stopwatch.Start();
                if (++i % 3 == 0)
                {
                    i = 1;
                    await Task.Run(() => DecayResources(fixedTimeStep * 3));
                }

                await UpdateAgents(_agents, fixedTimeStep).ConfigureAwait(false);
                // Render
                await Render(); //.ConfigureAwait(false);
                stopwatch.Stop();
                // Sleep the thread to maintain a constant frame rate
                var elapsedMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds;
                var sleepTime = fixedTimeStep * 1000.0f - elapsedMilliseconds;
                if (sleepTime > 0) await Task.Delay((int)sleepTime);
            }
    }

    public void OnMouseMove(object sender, MouseEventArgs e)
    {
        // If left button is pressed
        if (e.LeftButton != MouseButtonState.Pressed) return;
        // Add food with value 1f to location of mouse click
        var mousePosition = e.GetPosition(sender as UIElement);
        var mouseX = (float)mousePosition.X;
        var mouseY = (float)mousePosition.Y;
        if (!WithinBounds(mouseX, mouseY)) return;
        AddFood(new Point(mouseX, mouseY), 0.2f, 0.015f);
    }

    public void AddPheromone(string type, Point pos, float amount, float decayRate)
    {
        // Get key and check if resource exists
        var key = SimulationObjectMixin.KeyFor(type, (int)pos.X, (int)pos.Y);
        // Try get resource if exists and update its amount
        if (Resources.TryGetValue(key, out var res))
        {
            var currValue = res.Amount;
            res.Amount = currValue + amount;
            return;
        }

        // Otherwise get new pheromone from pool and add it.
        if (type == "pheromone")
        {
            Resources.TryAdd(key, _pheromoneResourcePool.GetObject(pos, amount, decayRate));
            return;
        }

        Resources.TryAdd(key, _pheromoneResourceReturnPool.GetObject(pos, amount, decayRate));
    }

    public async Task<IOption<float>> AttemptToTakeResourceAmount(string key, float maxAmount)
    {
        return await Task.Run(() =>
        {
            if (!Resources.TryGetValue(key, out var res)) return _noneFloat;
            lock (res)
            {
                var amount = Math.Min(res.Amount, maxAmount);
                res.Amount -= amount;
                if (!(res.Amount <= 0.08f)) return new Some<float>(amount);
                Resources.TryRemove(key, out _);
                RaiseResourceDepletedEvent(key);

                return new Some<float>(amount);
            }
        });
    }

    public void RaiseResourceDepletedEvent(string key)
    {
        ResourceDepleted.Invoke(this, new ResourceDepletedEventArgs(key));
    }

    private async Task UpdateAgents(IEnumerable<ISimulationAgent> agents, float deltaTime)
    {
        foreach (var agent in agents) await agent.Act(this, deltaTime);
    }

    private void DecayResources(float deltaTime)
    {
        var keysToRemove = new List<string>();
        // iterate over resources
        foreach (var res in Resources)
        {
            res.Value.Decay(deltaTime);
            if (res.Value.Amount <= 0.08f) keysToRemove.Add(res.Key);
        }

        foreach (var key in keysToRemove)
        {
            RaiseResourceDepletedEvent(key);
            Resources.TryRemove(key, out _);
        }
    }

    private void AddFood(Point pos, float amount, float decayRate)
    {
        // If a food-resource exists at the given position, then add the amount to it
        var key = SimulationObjectMixin.KeyFor("food", (int)pos.X, (int)pos.Y);
        if (Resources.TryGetValue(key, out var existingFood))
        {
            existingFood.Amount += amount;
            return;
        }
        var position = new Point((int)pos.X, (int)pos.Y);
        // Otherwise create a new food-resource and add it to the dictionary
        var food = new FoodResource(position, amount, decayRate);
        Resources.TryAdd(key, food);
    }

    private Task Render()
    {
        return Task.Run(() =>
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Delete all resource-representations queued for removal
                _canvas.RemoveResourcesToBeDeleted();
                _canvas.DrawHome();
                // Iterate over resource and draw them
                foreach (var res in Resources)
                {
                    var pos = new Point(res.Value.X, res.Value.Y);
                    switch (res.Value)
                    {
                        case FoodResource:
                            _canvas.DrawFood(res.Key, pos, res.Value.Amount);
                            break;
                        case PheromoneResource or PheromoneResourceReturn:
                            _canvas.DrawPheromone(res.Value.Type, res.Key, pos, res.Value.Amount);
                            break;
                    }
                }

                // Iterate over agents and draw them
                foreach (var agent in _agents)
                {
                    var pos = new Point(agent.State.X, agent.State.Y);
                    _canvas.DrawAnt(agent.Id, pos, agent.State.Orientation);
                    _canvas.DrawVisionCone(
                        agent.Id,
                        pos,
                        agent.State.Orientation,
                        agent.State.SensoryFieldRadius,
                        agent.State.SensoryFieldAngle
                    );
                }
            });
        });
    }
}