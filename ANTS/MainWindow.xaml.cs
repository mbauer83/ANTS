﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ANTS.Definitions;
using ANTS.Implementations;

namespace ANTS;

public partial class MainWindow
{
    private const int SimulationCanvasWidth = 1000;
    private const int SimulationCanvasHeight = 600;
    private const int HomeX = SimulationCanvasWidth / 4;
    private const int HomeY = SimulationCanvasHeight / 2;
    private const float FoodDecayRate = 0.002f;
    private const int FoodClusterX = (int)(SimulationCanvasWidth * 0.75f);
    private const int FoodClusterY = (int)(SimulationCanvasHeight * 0.5f);

    private readonly SimulationArena _arena;

    public MainWindow()
    {
        Width = SimulationCanvasWidth + 50;
        Height = SimulationCanvasHeight + 200;
        MinWidth = Width;
        MinHeight = Height;
        InitializeComponent();

        ThreadPool.GetMinThreads(out _, out var minCompletionPortThreads);
        ThreadPool.SetMaxThreads(Environment.ProcessorCount * 2, minCompletionPortThreads);

        // Cluster food around the middle of the right half of the canvas
        var resourcesDict = new ConcurrentDictionary<string, ISimulationResource>();
        var random = new Random();
        for (var i = 0; i < 400; i++)
        {
            // Amount is random float between 0.1 and 1
            var amount = (float)(random.NextDouble() * 0.9 + 0.1);
            var x = FoodClusterX + (int)(random.NextDouble() * 100 - 50);
            var y = FoodClusterY + (int)(random.NextDouble() * 100 - 50);
            var position = new Point(x, y);
            var food = new FoodResource(position, amount, FoodDecayRate);
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

        var pheromoneResourcePool = new PheromoneResourcePool(SimulationCanvasWidth * SimulationCanvasHeight / 2,
            SimulationCanvasWidth * SimulationCanvasHeight);
        var pheromoneResourceReturnPool = new PheromoneResourceReturnPool(
            SimulationCanvasWidth * SimulationCanvasHeight / 2, SimulationCanvasWidth * SimulationCanvasHeight);

        // Create 1000 agents starting at the middle of the left half of the canvas
        var agents = new List<ISimulationAgent>();
        for (var i = 0; i < 50; i++)
        {
            // random orientation between 0f and TwoPi
            var orientation = (float)(random.NextDouble() * 2f * float.Pi);
            var antState = new AntState(HomeX, HomeY, orientation, 120f, 0f);
            var ant = new Ant(i.ToString(), antState, (HomeX, HomeY), random);
            agents.Add(ant);
        }

        // Crate the parent-canvas to contain buttons, text and the simulation canvas
        var parentCanvas = new Canvas();
        var simulationCanvasTopPosition = Height - SimulationCanvasHeight;
        parentCanvas.Width = SimulationCanvasWidth;
        parentCanvas.Height = SimulationCanvasHeight + simulationCanvasTopPosition;
        
        // Set font size to percentage of canvas height
        var fontSize = Math.Max(16, 0.02 * SimulationCanvasHeight);
        
        // Add label with hint for drawing food
        var drawFoodLabel = new Label
        {
            Content = "Hold down left mouse button and drag to draw food",
            Height = fontSize + 20,
            Width = 500,
            FontSize = fontSize
        };
        // Position on top left corner
        Canvas.SetTop(drawFoodLabel, Math.Max(50, 0.02 * SimulationCanvasHeight));
        Canvas.SetLeft(drawFoodLabel, Math.Max(50, 0.02 * SimulationCanvasWidth));
        // Set z-level of button
        Panel.SetZIndex(drawFoodLabel, 3);
        // Add to parent canvas
        parentCanvas.Children.Add(drawFoodLabel);

        // Add Pause/Run button
        var pauseRunButton = new Button
        {
            Content = "Pause/Run",
            Height = 30,
            Width = 100
        };
        pauseRunButton.Click += TogglePause;
        // Add to parent Canvas
        // position button with half of simulationCanvasTopPosition as top position and 2% of width or at least 50px right margin
        Canvas.SetTop(pauseRunButton, Math.Max(50, 0.02 * SimulationCanvasHeight));
        Canvas.SetRight(pauseRunButton, Math.Max(50, 0.02 * SimulationCanvasWidth));
        // Set z-level of button
        Panel.SetZIndex(pauseRunButton, 3);
        parentCanvas.Children.Add(pauseRunButton);

        // Create the SimulationCanvas object and attach it to the parentCanvas 
        var canvas = new SimulationCanvas(SimulationCanvasWidth, SimulationCanvasHeight);
        Canvas.SetTop(canvas, simulationCanvasTopPosition / 2);
        Canvas.SetLeft(canvas, 0);
        parentCanvas.Children.Add(canvas);

        Content = parentCanvas;

        // Create the SimulationArena object
        var arena = new SimulationArena(
            SimulationCanvasWidth,
            SimulationCanvasHeight,
            resourcesDict,
            agents,
            canvas,
            pheromoneResourcePool,
            pheromoneResourceReturnPool
        );
        _arena = arena;

        // Attach SimulationArena::OnMouseMove
        canvas.MouseMove += arena.OnMouseMove;

        // Start the simulation
        Task.Run(() => arena.RunGameLoop());
    }

    private void TogglePause(object sender, EventArgs e)
    {
        _arena.TogglePause();
    }
}