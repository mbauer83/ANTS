using System;
using System.Collections.Concurrent;
using System.Windows;

namespace AntColonySimulation.implementations;

public class PheromoneResourcePool
{
    private readonly ConcurrentQueue<PheromoneResource> _pool;
    private readonly int _maxCapacity;

    public PheromoneResourcePool(int initialCapacity, int maxCapacity)
    {
        _pool = new ConcurrentQueue<PheromoneResource>();
        _maxCapacity = maxCapacity;

        for (int i = 0; i < initialCapacity; i++)
        {
            _pool.Enqueue(new PheromoneResource(new Point(0d, 0d), 0f, 0f));
        }
    }

    public PheromoneResource GetObject(Point position, float amount, float decayRate)
    {
        if (!_pool.TryDequeue(out PheromoneResource? resource))
        {
            if (_pool.Count < _maxCapacity)
            {
                resource = new PheromoneResource(position, amount, decayRate);
            }
            else
            {
                throw new InvalidOperationException($"Pool capacity exceeded. Max capacity: {_maxCapacity}");
            }
        }

        resource.Position = position;
        resource.Amount = amount;
        resource.DecayRate = decayRate;
        return resource;
    }

    public void ReturnObject(PheromoneResource resource)
    {
        // Add the resource back to the pool
        _pool.Enqueue(resource);
    }
}