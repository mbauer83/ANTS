using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using WpfApp1.utils.fn;
using WpfApp1.utils.geometry;

namespace WpfApp1;

public class SimulationArena<T> where T : ISimulationAgentState
{
    private const float TwoPi = 2 * float.Pi;
    public int Width { get; }
    public int Height { get; }
    private Dictionary<string, ISimulationResource> _resources;
    private ConcurrentQueue<ResourceAccessRequest<T>> _resourceAccessRequests;
    private List<ISimulationAgent<T>> _agents;
    private bool _run = false;
    private SimulationCanvas _canvas;
    public Point Home { get; }

    public async void Stop()
    {
        _run = false;
    }

    public SimulationArena(
        int width,
        int height,
        Dictionary<string, ISimulationResource> resources,
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
    }

    public List<(ISimulationResource, float)> ResourcesInSensoryField(
        ISimulationAgent<T> agent,
        string resourceType,
        float valueThreshold = 0f
    ) {
        var list = (
            from res in this._resources
            where res.Value.Type == resourceType &&
                  res.Value.Amount >= valueThreshold &&
                  agent.WithinSensoryField(res.Value.X, res.Value.Y)
            select (res.Value, Geometry2D.EuclideanDistance(agent.State.X, agent.State.Y, res.Value.X, res.Value.Y))
        ).ToList();
        list.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
        return list;
    }

    public List<(ISimulationAgent<T>, float)> AgentsInSensoryField(
        ISimulationAgent<T> agent
    ) {
        var list = (
            from otherAgent in this._agents.AsParallel()
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
        this._resourceAccessRequests.Enqueue(req);
    }
    
    private async void ProcessResourceAccessRequest(ResourceAccessRequest<T> req)
    {
        // If resource does not exist or is locked, then resolve request with None
        var key = SimulationObjectMixin.KeyFor(req.Type, req.X, req.Y);
        var resExists = this._resources.ContainsKey(key);
        var res = resExists ? this._resources[key] : null;
        if (!resExists|| (res.LockedByAgentId.IsSome() && res.LockedByAgentId.Get() != req.Agent.Id))
        {
            req.Agent.SolveResourceAccessRequest(new None<ISimulationResource>());
            return;
        }

        // Otherwise split the resource, set the right result in the dictionary
        // and resolve the request with the left result
        var (left, right) = this._resources[key].Split(req.Amount);
        if (right is Some<ISimulationResource> rightSome)
        {
            _resources[key] = rightSome.Value;
        }
        else
        {
            _resources.Remove(key);
            //_resources[key] = _resources[key].WithAmount(0f);
        }

        req.Agent.SolveResourceAccessRequest(left);
    }

    public bool WithinBounds(float x, float y)
    {
        var buffer = 5f;
        return x >= buffer && x <= (Width - buffer) && y >= buffer && y <= (Height - buffer);
    }

    private async Task UpdateAgents(List<ISimulationAgent<T>> agents, float deltaTime)
    {
        List<Task> tasks = new List<Task>();
        foreach (var agent in agents)
        {
            tasks.Add(agent.Act(this, deltaTime));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessResourceAccessRequests()
    {
        List<Task> tasks = new List<Task>();
        while (this._resourceAccessRequests.TryDequeue(out var req))
        {
            tasks.Add(Task.Run(() => this.ProcessResourceAccessRequest(req)));
        }

        await Task.WhenAll(tasks);
    }

    public void DecayResources(float deltaTime)
    {
        var keysToRemove = new List<string>();
        foreach (var res in this._resources.Values)
        {
            res.Decay(deltaTime);
            if (res.Amount <= 0.08f)
            {
                keysToRemove.Add(res.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            this._resources.Remove(key);
            _canvas.RemoveElement(key);
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
        var deltaTime = fixedTimeStep;

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

            deltaTime = elapsedMilliseconds / 1000f;
        }
    }

    private Task Render()
    {
        return Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _canvas.DrawHome();
                // Iterate over resource and draw them
                foreach (var res in this._resources.Values)
                {
                    var pos = new Point(res.X, res.Y);
                    if (res is FoodResource)
                    {
                        _canvas.DrawFood(res.Key, pos, res.Amount);
                    }
                    else if (res is PheromoneResource || res is PheromoneResourceReturn)
                    {
                        _canvas.DrawPheromone(res.Type, res.Key, pos, res.Amount);
                    }
                }

                // Iterate over agents and draw them
                foreach (var agent in this._agents)
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