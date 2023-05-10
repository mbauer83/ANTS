using System;
using System.Collections.Concurrent;
using System.Windows;

namespace AntColonySimulation.Implementations;

public class PheromoneResourceReturnPool
{
    private readonly ConcurrentQueue<PheromoneResourceReturn> _pool;
    private readonly int _maxCapacity;

    public PheromoneResourceReturnPool(int initialCapacity, int maxCapacity)
    {
        _pool = new ConcurrentQueue<PheromoneResourceReturn>();
        _maxCapacity = maxCapacity;

        for (int i = 0; i < initialCapacity; i++)
        {
            _pool.Enqueue(new PheromoneResourceReturn(new Point(0d, 0d), 0f, 0f));
        }
    }

    public PheromoneResourceReturn GetObject(Point position, float amount, float decayRate)
    {
        if (!_pool.TryDequeue(out PheromoneResourceReturn? resource))
        {
            if (_pool.Count < _maxCapacity)
            {
                resource = new PheromoneResourceReturn(position, amount, decayRate);
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

    public void ReturnObject(PheromoneResourceReturn resource)
    {
        // Add the resource back to the pool
        _pool.Enqueue(resource);
    }
}