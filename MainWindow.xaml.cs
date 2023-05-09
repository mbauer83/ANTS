using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using AntColonySimulation.definitions;
using AntColonySimulation.implementations;

namespace AntColonySimulation
{
    public partial class MainWindow : Window
    {
        
        private const int WindowWidth = 1000;
        private const int WindowHeight = 600;
        private const float FoodDecayRate = 0.02f;
        private const int FoodClusterX = (int)(WindowWidth * 0.75f);
        private const int FoodClusterY = (int)(WindowHeight * 0.5f);
        
        public MainWindow()
        {

            InitializeComponent();
            
            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads);
            ThreadPool.SetMinThreads(16, minCompletionPortThreads);
            ThreadPool.SetMaxThreads(32, minCompletionPortThreads);

            // Cluster food around the middle of the right half of the canvas
            var resourcesDict = new ConcurrentDictionary<string, ISimulationResource>();
            var random = new Random();
            for (var i = 0; i < 400; i++)
            {
                // Amount is random float between 0.1 and 1
                var amount = (float)(random.NextDouble() * 0.9 + 0.1);
                var x = FoodClusterX + (int)(random.NextDouble() * 100 - 50);
                var y = FoodClusterY + (int)(random.NextDouble() * 100 - 50);
                var food = new FoodResource(x, y, amount, FoodDecayRate);
                var key = food.Key;
                if (resourcesDict.ContainsKey(key))
                {
                    var currResource = resourcesDict[key];
                    var newResource = currResource.WithAmount(currResource.Amount + food.Amount);
                    resourcesDict[key] = newResource;
                }
                else
                {
                    resourcesDict.TryAdd(key, food);
                }
            }
            
            var pheromoneResourcePool = new PheromoneResourcePool(WindowWidth * WindowHeight, WindowWidth * WindowHeight);
            var pheromoneResourceReturnPool = new PheromoneResourceReturnPool(WindowWidth * WindowHeight, WindowWidth * WindowHeight);

            // Create 1000 agents starting at the middle of the left half of the canvas
            var homeX = WindowWidth / 4;
            var homeY = WindowHeight / 2;
            var agents = new List<ISimulationAgent<AntState>>();
            for (var i = 0; i < 50; i++)
            {
                // random orientation between 0f and TwoPi
                var orientation = (float)(random.NextDouble()  * 2f * float.Pi);
                var antState = new AntState(homeX, homeY, orientation, 180f, 0f);
                var ant = new Ant(i.ToString(), antState, (homeX, homeY), random, pheromoneResourcePool, pheromoneResourceReturnPool);
                agents.Add(ant);
            }

            // Create the SimulationCanvas object
            var canvas = new SimulationCanvas(WindowWidth, WindowHeight);
            Content = canvas;

            // Create the SimulationArena object
            var arena = new SimulationArena<AntState>(
                WindowWidth,
                WindowHeight,
                resourcesDict,
                agents,
                canvas,
                pheromoneResourcePool,
                pheromoneResourceReturnPool
            );
            
            // Attach SimulationArena::OnLeftMouseUp as left mouse up event handler
            canvas.MouseMove += arena.OnMouseMove;
            
            // Start the simulation
            arena.RunGameLoop();
        }
    }
}