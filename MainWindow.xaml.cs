using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AntColonySimulation.Definitions;
using AntColonySimulation.Implementations;

namespace AntColonySimulation
{
    public partial class MainWindow : Window
    {
        
        private const int SimulationCanvasWidth     = 1000;
        private const int SimulationCanvasHeight    = 600;
        private const float FoodDecayRate = 0.02f;
        private const int FoodClusterX    = (int)(SimulationCanvasWidth * 0.75f);
        private const int FoodClusterY    = (int)(SimulationCanvasHeight * 0.5f);
        
        private SimulationArena<AntState> _arena;

        public MainWindow()
        {

            Width = SimulationCanvasWidth + 50;
            Height = SimulationCanvasHeight + 200;
            MinWidth = Width;
            MinHeight = Height;
            InitializeComponent();
            
            ThreadPool.GetMinThreads(out var _, out var minCompletionPortThreads);
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
            
            var pheromoneResourcePool = new PheromoneResourcePool(SimulationCanvasWidth * SimulationCanvasHeight / 2, SimulationCanvasWidth * SimulationCanvasHeight);
            var pheromoneResourceReturnPool = new PheromoneResourceReturnPool(SimulationCanvasWidth * SimulationCanvasHeight / 2, SimulationCanvasWidth * SimulationCanvasHeight);

            // Create 1000 agents starting at the middle of the left half of the canvas
            var homeX = SimulationCanvasWidth / 4;
            var homeY = SimulationCanvasHeight / 2;
            var agents = new List<ISimulationAgent<AntState>>();
            for (var i = 0; i < 50; i++)
            {
                // random orientation between 0f and TwoPi
                var orientation = (float)(random.NextDouble()  * 2f * float.Pi);
                var antState = new AntState(homeX, homeY, orientation, 120f, 0f);
                var ant = new Ant(i.ToString(), antState, (homeX, homeY), random);
                agents.Add(ant);
            }
            
            // Crate the parent-canvas to contain buttons, text and the simulation canvas
            var parentCanvas = new Canvas();
            var simulationCanvasTopPosition = Height - SimulationCanvasHeight;
            parentCanvas.Width = SimulationCanvasWidth;
            parentCanvas.Height = SimulationCanvasHeight + simulationCanvasTopPosition;
            
            // Add Pause/Run button
            var pauseRunButton = new Button()
            {
                Content = "Pause/Run",
            };
            pauseRunButton.Height = 30;
            pauseRunButton.Click += TogglePause;
            // Add to parent Canvas
            // position button with half of simulationCanvasTopPosition as top position and 2% of width or at least 50px right margin
            Canvas.SetTop(pauseRunButton, simulationCanvasTopPosition / 2 - pauseRunButton.Height);
            Canvas.SetRight(pauseRunButton, Math.Max(50, 0.02 * SimulationCanvasWidth));
            // Set z-level of button
            Canvas.SetZIndex(pauseRunButton, 3);
            parentCanvas.Children.Add(pauseRunButton);
            
            // Create the SimulationCanvas object and attach it to the parentCanvas 
            var canvas = new SimulationCanvas(SimulationCanvasWidth, SimulationCanvasHeight);
            Canvas.SetTop(canvas, simulationCanvasTopPosition / 2);
            Canvas.SetLeft(canvas, 0);
            parentCanvas.Children.Add(canvas);
            
            Content = parentCanvas;

            // Create the SimulationArena object
            var arena = new SimulationArena<AntState>(
                SimulationCanvasWidth,
                SimulationCanvasHeight,
                resourcesDict,
                agents,
                canvas,
                pheromoneResourcePool,
                pheromoneResourceReturnPool
            );
            _arena = arena;
            
            // Attach SimulationArena::OnMouseMove as left mouse up event handler
            canvas.MouseMove += arena.OnMouseMove;
            
            // Start the simulation
            arena.RunGameLoop();
        }
        
        public void TogglePause(object sender, EventArgs e)
        {
            _arena.TogglePause();
        }
    }
}