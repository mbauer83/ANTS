using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private SimulationArena<AntState> _arena;

        public MainWindow()
        {

            InitializeComponent();
            var width = 1000;
            var height = 600;
            var foodDecayRate = 0.015f;

            // Cluster food around the middle of the right half of the canvas
            var foodClusterX = (int)(width * 0.75f);
            var foodClusterY = (int)(height * 0.5f);
            var resourcesDict = new Dictionary<string, ISimulationResource>();
            var random = new Random();
            for (var i = 0; i < 400; i++)
            {
                // Amount is random float between 0.1 and 1
                var amount = (float)(random.NextDouble() * 0.9 + 0.1);
                var x = foodClusterX + (int)(random.NextDouble() * 100 - 50);
                var y = foodClusterY + (int)(random.NextDouble() * 100 - 50);
                var food = new FoodResource(x, y, amount, foodDecayRate);
                var key = food.Key;
                if (resourcesDict.ContainsKey(key))
                {
                    var currResource = resourcesDict[key];
                    var newResource = currResource.WithAmount(currResource.Amount + food.Amount);
                    resourcesDict[key] = newResource;
                }
                else
                {
                    resourcesDict.Add(key, food);
                }
            }

            // Create 1000 agents starting at the middle of the left half of the canvas
            var homeX = width / 4;
            var homeY = height / 2;
            var agents = new List<ISimulationAgent<AntState>>();
            for (var i = 0; i < 50; i++)
            {
                // random orientation between 0f and TwoPi
                var orientation = (float)(random.NextDouble()  * 2f * float.Pi);
                var antState = new AntState(homeX, homeY, orientation, 180f, 0f);
                var ant = new Ant(i.ToString(), antState, (homeX, homeY), random);
                agents.Add(ant);
            }

            // Create the SimulationCanvas object
            var canvas = new SimulationCanvas(width, height);
            Content = canvas;

            // Create the SimulationArena object
            var arena = new SimulationArena<AntState>(
                width,
                height,
                resourcesDict,
                agents,
                canvas
            );
            
            // Start the simulation
            arena.RunGameLoop();
        }
    }
}