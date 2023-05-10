using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AntColonySimulation.Definitions;
using AntColonySimulation.Utils.Geometry;

namespace AntColonySimulation.Implementations;

public class SimulationArena<T> : ISimulationArena<T> where T : ISimulationAgentState
{
    public int Width { get; }
    public int Height { get; }
    public Point Home { get; }
    public ConcurrentDictionary<string, ISimulationResource> Resources { get; }
    private readonly List<ISimulationAgent<T>> _agents;
    private readonly SimulationCanvas _canvas;
    private readonly PheromoneResourcePool _pheromoneResourcePool;
    private readonly PheromoneResourceReturnPool _pheromoneResourceReturnPool;

    private bool _run;
    public void TogglePause()
    {
        _run = !_run;
    }

    public SimulationArena(
        int width,
        int height,
        ConcurrentDictionary<string, ISimulationResource> resources,
        List<ISimulationAgent<T>> agents,
        SimulationCanvas canvas,
        PheromoneResourcePool pheromoneResourcePool,
        PheromoneResourceReturnPool pheromoneResourceReturnPool
    )
    {
        Width = width;
        Height = height;
        Resources = resources;
        _agents = agents;
        _canvas = canvas;
        Home = new Point(width / 4f, height / 2f);
        ResourceDepleted += _canvas.OnResourceDepleted;
        _pheromoneResourcePool = pheromoneResourcePool;
        _pheromoneResourceReturnPool = pheromoneResourceReturnPool;
    }

    public List<(ISimulationResource, float, float)> ResourcesInSensoryField(
        ISimulationAgent<T> agent,
        string resourceType,
        float exclusiveLowerLimit = 0f,
        float? exclusiveUpperLimit = null
    ) {
        var list = new List<(ISimulationResource, float, float)>();
        foreach (var res in Resources)
        {
            if (res.Value.Type == resourceType &&
                res.Value.Amount > exclusiveLowerLimit &&
                (exclusiveUpperLimit == null || res.Value.Amount < exclusiveUpperLimit) &&
                agent.WithinSensoryField(res.Value.X, res.Value.Y)
            )
            {
                var distance = Geometry2D.EuclideanDistance(agent.State.X, agent.State.Y, res.Value.X, res.Value.Y);
                var relativeOrientation = MathF.Atan2(res.Value.Y - agent.State.Y, res.Value.X - agent.State.X);
                list.Add((
                    res.Value, 
                    distance,
                    relativeOrientation
                    ));
            }
        }
        list.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
        return list;
    }

    public List<(ISimulationAgent<T>, float)> AgentsInSensoryField(
        ISimulationAgent<T> agent
    ) {
        var list = (
            from otherAgent in _agents.AsParallel()
            where agent.WithinSensoryField(otherAgent.State.X, otherAgent.State.Y) && otherAgent.Id != agent.Id
            select (
                otherAgent,
                Geometry2D.EuclideanDistance(
                    agent.State.X,
                    agent.State.Y,
                    otherAgent.State.X,
                    otherAgent.State.Y
                )
            )
        ).ToList();
        list.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
        return list;
    }

    public bool WithinBounds(float x, float y)
    {
        var buffer = 5f;
        return x >= buffer && x <= (Width - buffer) && y >= buffer && y <= (Height - buffer);
    }

    private async Task UpdateAgents(List<ISimulationAgent<T>> agents, float deltaTime)
    {
        //List<Task> tasks = new List<Task>();
        //AgentUpdateContext<T> ctx = new(this, deltaTime);
        
        Func<ISimulationAgent<T>, Task> fn = agent =>
        {
            return Task.Run(() =>
            {
                agent.Act(this, deltaTime);
            });
        };

        async Task AwaitPartition(IEnumerator<ISimulationAgent<T>> partition)
        {
            using (partition)
            {
                while (partition.MoveNext())
                {
                    await fn(partition.Current);
                }
            }    
        }
        
        await Task.WhenAll(
            Partitioner
                .Create(agents)
                .GetPartitions(Environment.ProcessorCount)
                .AsParallel()
                .Select(AwaitPartition)
        );
        

        //foreach (var agent in agents)
        //{
        //    AgentTaskComponents<T> components = new(agent, ctx);
        //    var t = Task.Factory.StartNew(state =>
        //        {
        //            if (state is AgentTaskComponents<T>(var localAgent, var agentUpdateContext))
        //            {
        //                var localArena = agentUpdateContext.Arena;
        //                var localDeltaTime = agentUpdateContext.DeltaTime;
        //                localAgent.Act(localArena, ref localDeltaTime);
        //            }

        //            return Task.CompletedTask;
        //        }, components, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default)
        //        .Unwrap();
        //    tasks.Add(t);
        //}

        //await Task.WhenAll(tasks);
    }

    //private void DecayResources(float deltaTime)
    //{
    //    var keysToRemove = new List<string>();

    //    foreach (var res in _resources)
    //    {
    //        res.Value.Decay(deltaTime);
    //        if (res.Value.Amount <= 0.08f)
    //        {
    //            keysToRemove.Add(res.Key);
    //        }
    //    }

    //    foreach (var key in keysToRemove)
    //    {
    //        if (!_resources.TryRemove(key, out var res)) continue;
    //        switch (res)
    //        {
    //            case PheromoneResource pheromoneValue:
    //                _pheromoneResourcePool.ReturnObject(pheromoneValue);
    //                break;
    //            case PheromoneResourceReturn returnPheromoneValue:
    //                _pheromoneResourceReturnPool.ReturnObject(returnPheromoneValue);
    //                break;
    //        }
    //            
    //        RaiseResourceDepletedEvent(key);
    //    }
    //}
    
    private void DecayResources(float deltaTime)
    {
        var keysToRemove = new List<string>();
        // iterate over resources
        Resources.AsParallel().ForAll(res =>
        {
            res.Value.Decay(deltaTime);
            if (res.Value.Amount <= 0.08f)
            {
                keysToRemove.Add(res.Key);
            }
        });
        foreach (var key in keysToRemove)
        {
            RaiseResourceDepletedEvent(key);
            Resources.TryRemove(key, out _);
        }
    }
    
    //private void DecayResources(float deltaTime)
    //{
    //    var keysToRemove = new List<string>();
    //    Func<KeyValuePair<string, ISimulationResource>, Task> fn = res =>
    //    {
    //        return Task.Run(() =>
    //        {
    //            res.Value.Decay(deltaTime);
    //            if (res.Value.Amount <= 0.08f)
    //            {
    //                keysToRemove.Add(res.Key);
    //            }
    //        });
    //    };

    //    async Task AwaitPartition(IEnumerator<KeyValuePair<string, ISimulationResource>> partition)
    //    {
    //        using (partition)
    //        {
    //            while (partition.MoveNext())
    //            {
    //                await fn(partition.Current);
    //            }
    //        }    
    //    }
    //    
    //    Task.WhenAll(
    //        Partitioner
    //            .Create(_resources)
    //            .GetPartitions(24)
    //            .AsParallel()
    //            .Select(AwaitPartition)
    //    );
    //    foreach (var key in keysToRemove)
    //    {
    //        _resources.TryRemove(key, out _);
    //        RaiseResourceDepletedEvent(key);
    //    }
    //}

    public void AddFood(Point pos, float amount, float decayRate)
    {
        // If a food-resource exists at the given position, then add the amount to it
        var key = SimulationObjectMixin.KeyFor("food", (int)pos.X, (int)pos.Y);
        if (Resources.TryGetValue(key, out var existingFood))
        {
            existingFood.Amount += amount;
            return;
        }
        // Otherwise create a new food-resource and add it to the dictionary
        var food = new FoodResource((int)pos.X, (int)pos.Y, amount, decayRate);
        Resources.TryAdd(key, food);
    }

    public async Task RunGameLoop(int fps = 60)
    {
        _run = true;
        var stopwatch = new Stopwatch();
        var fixedTimeStep = 1f / fps;
        var i = 0;
        while (true)
        {
            if (_run)
            {
                stopwatch.Reset();
                stopwatch.Start();
                if (++i % 3 == 0)
                {
                    i = 1;
                    DecayResources(fixedTimeStep * 3);
                }

                await UpdateAgents(_agents, fixedTimeStep).ConfigureAwait(false);
                // Render
                await Render(); //.ConfigureAwait(false);
                stopwatch.Stop();
                // Sleep the thread to maintain a constant frame rate
                var elapsedMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds;
                var sleepTime = (fixedTimeStep * 1000.0f) - elapsedMilliseconds;
                if (sleepTime > 0)
                {
                    await Task.Delay((int)sleepTime);
                }
            }
        }
    }

    private Task Render()
    {
        return Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Delete all resource-representations queued for removal
                _canvas.RemoveResourcesToBeDeleted();
                _canvas.DrawHome();
                // Iterate over resource and draw them
                foreach (var res in Resources)
                {
                    var pos = new Point(res.Value.X, res.Value.Y);
                    if (res.Value is FoodResource)
                    {
                        _canvas.DrawFood(res.Key, pos, res.Value.Amount);
                    }
                    else if (res.Value is PheromoneResource or PheromoneResourceReturn)
                    {
                        _canvas.DrawPheromone(res.Value.Type, res.Key, pos, res.Value.Amount);
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
    
    public void RaiseResourceDepletedEvent(string key)
    {
        ResourceDepleted?.Invoke(this, new ResourceDepletedEventArgs(key));
    }
    public EventHandler<ResourceDepletedEventArgs> ResourceDepleted;
    
}