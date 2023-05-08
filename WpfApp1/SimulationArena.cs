using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfApp1.utils.fn;
using WpfApp1.utils.geometry;

namespace WpfApp1;

public class SimulationArena<T> where T : ISimulationAgentState
{
    public int Width { get; }
    public int Height { get; }
    private readonly ConcurrentDictionary<string, ISimulationResource> _resources;
    private readonly ConcurrentQueue<ResourceAccessRequest<T>> _resourceAccessRequests;
    private readonly List<ISimulationAgent<T>> _agents;
    private bool _run;
    private readonly SimulationCanvas _canvas;
    public Point Home { get; }

    public void Stop()
    {
        _run = false;
    }

    public SimulationArena(
        int width,
        int height,
        ConcurrentDictionary<string, ISimulationResource> resources,
        List<ISimulationAgent<T>> agents,
        SimulationCanvas canvas
    )
    {
        Width = width;
        Height = height;
        _resources = resources;
        _agents = agents;
        _resourceAccessRequests = new ConcurrentQueue<ResourceAccessRequest<T>>();
        _canvas = canvas;
        Home = new Point(width / 4f, height / 2f);
        ResourceDepleted += _canvas.OnResourceDepleted;
    }

    public List<(ISimulationResource, float, float)> ResourcesInSensoryField(
        ISimulationAgent<T> agent,
        string resourceType,
        float exclusiveLowerLimit = 0f,
        float? exclusiveUpperLimit = null
    ) {
        var list = new List<(ISimulationResource, float, float)>();
        foreach (var res in _resources)
        {
            if (res.Value.Type == resourceType &&
                res.Value.Amount > exclusiveLowerLimit &&
                (exclusiveUpperLimit == null || res.Value.Amount < exclusiveUpperLimit) &&
                agent.WithinSensoryField(res.Value.X, res.Value.Y))
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

    public void AddResourceAccessRequest(ResourceAccessRequest<T> req)
    {
        _resourceAccessRequests.Enqueue(req);
    }
    
    private void ProcessResourceAccessRequest(ResourceAccessRequest<T> req)
    {
        // If resource does not exist or is locked, then resolve request with None
        var key = SimulationObjectMixin.KeyFor(req.Type, req.X, req.Y);
        _resources.TryGetValue(key, out var res);
        var maybeResource = IOption<ISimulationResource>.FromNullable(res);
        maybeResource.Match(
            t =>
            {
                var resource = t.Item1;
                var request = t.Item2.Item1;
                var localKey = t.Item2.Item2;
                if (resource.LockedByAgentId.IsSome() && resource.LockedByAgentId.Get() != request.Agent.Id)
                {
                    request.Agent.SolveResourceAccessRequest(new None<ISimulationResource>());
                    return;
                }
                // Otherwise split the resource, set the right result in the dictionary
                // and resolve the request with the left result
                var (left, right) = resource.Split(request.Amount);
                if (right is Some<ISimulationResource> rightSome)
                {
                    _resources.TryUpdate(localKey, rightSome.Value, resource);
                }
                else
                {
                    _resources.TryRemove(localKey, out _);
                    RaiseResourceDepletedEvent(localKey);
                }
                request.Agent.SolveResourceAccessRequest(left);
                
            },
            (_) => {},
            (req, key)
        );
    }

    public bool WithinBounds(float x, float y)
    {
        var buffer = 5f;
        return x >= buffer && x <= (Width - buffer) && y >= buffer && y <= (Height - buffer);
    }

    private async Task UpdateAgents(List<ISimulationAgent<T>> agents, float deltaTime)
    {
        List<Task> tasks = new List<Task>();
        AgentUpdateContext<T> ctx = new(this, deltaTime);
        

        foreach (var agent in agents)
        {
            AgentTaskComponents<T> components = new(agent, ctx);
            var t = Task.Factory.StartNew(state =>
                {
                    if (state is AgentTaskComponents<T>(var localAgent, var agentUpdateContext))
                    {
                        var localArena = agentUpdateContext.Arena;
                        var localDeltaTime = agentUpdateContext.DeltaTime;
                        localAgent.Act(localArena, localDeltaTime);
                    }

                    return Task.CompletedTask;
                }, components, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default)
                .Unwrap();
            tasks.Add(t);
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessResourceAccessRequests()
    {
        List<Task> tasks = new List<Task>();
        
        while (_resourceAccessRequests.TryDequeue(out var req))
        {
            ResourceAccessRequestTaskComponents<T> components = new(this, req);
            var t = Task.Factory.StartNew(state =>
                {
                    if (state is ResourceAccessRequestTaskComponents<T>(var localArena, var localRequest))
                    {
                        localArena.ProcessResourceAccessRequest(localRequest);
                    }

                    return Task.CompletedTask;
                }, components, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default)
                .Unwrap();
            tasks.Add(t);
        }

        await Task.WhenAll(tasks);
    }

    private void DecayResources(float deltaTime)
    {
        var keysToRemove = new List<string>();
        // iterate over resources
        _resources.AsParallel().ForAll(res =>
        {
            res.Value.Decay(deltaTime);
            if (res.Value.Amount <= 0.08f)
            {
                keysToRemove.Add(res.Key);
            }
        });
        foreach (var key in keysToRemove)
        {
            _resources.Remove(key, out _);
            RaiseResourceDepletedEvent(key);
        }
    }

    public void AddPheromone(ISimulationResource res)
    {
        // If a pheromone-resource exists at the given position, then add the amount to it
        var key = res.Key;
        if (_resources.ContainsKey(key))
        {
            var currValue = _resources[key].Amount;
            _resources[key] = _resources[key].WithAmount(currValue + res.Amount);
            return;
        }
        _resources[key] = res;
    }

    public async void RunGameLoop(int fps = 60)
    {
        _run = true;
        var stopwatch = new Stopwatch();
        var fixedTimeStep = 1f / fps;

        while (_run)
        {
            stopwatch.Reset();
            stopwatch.Start();
            DecayResources(fixedTimeStep);
            await ProcessResourceAccessRequests();
            await UpdateAgents(_agents, fixedTimeStep);
            // Render
            await Render();
            stopwatch.Stop();
            // Sleep the thread to maintain a constant frame rate
            var elapsedMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds;
            var sleepTime = (fixedTimeStep * 1000.0f) - elapsedMilliseconds;
            if (sleepTime > 0)
            {
                Thread.Sleep((int)sleepTime);
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
                foreach (var res in _resources.Values)
                {
                    var pos = new Point(res.X, res.Y);
                    if (res is FoodResource)
                    {
                        _canvas.DrawFood(res.Key, pos, res.Amount);
                    }
                    else if (res is PheromoneResource or PheromoneResourceReturn)
                    {
                        _canvas.DrawPheromone(res.Type, res.Key, pos, res.Amount);
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
        var food = new FoodResource(mouseX, mouseY, 0.2f, 0.015f);
        AddPheromone(food);
    }
    
    private void RaiseResourceDepletedEvent(string key)
    {
        ResourceDepleted?.Invoke(this, new ResourceDepletedEventArgs(key));
    }
    public EventHandler<ResourceDepletedEventArgs> ResourceDepleted;
    
}