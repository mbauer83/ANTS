using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AntColonySimulation.Implementations
{
    public class ResourceDepletedEventArgs : EventArgs
    {
        public string Key { get; }
        public ResourceDepletedEventArgs(string key)
        {
            Key = key;
        }
    }
    
    public class SimulationCanvas : Canvas
    {
        public EventHandler<ResourceDepletedEventArgs>? ResourceDepleted;
        
        private readonly ConcurrentQueue<string> _deleteResourceQueue = new ConcurrentQueue<string>();

        private readonly Dictionary<string, UIElement> _elements = new Dictionary<string, UIElement>();

        private Dictionary<Color, SolidColorBrush> _brushes = new Dictionary<Color, SolidColorBrush>();
        public SimulationCanvas(int width, int height)
        {
            Background = Brushes.White;
            // set size
            Width = width;
            Height = height;
        }

        public void OnResourceDepleted(object? sender, ResourceDepletedEventArgs e)
        {
            _deleteResourceQueue.Enqueue(e.Key);
        }

        public void AddElement(string name, UIElement element)
        {
            _elements.Add(name, element);
            Children.Add(element);
        }

        public void RemoveElement(string name)
        {
            if (!_elements.TryGetValue(name, out var element)) return;
            Children.Remove(element);
            _elements.Remove(name);
        }

        public void ClearElements()
        {
            Children.Clear();
            _elements.Clear();
        }

        public void DrawAnt(string id, Point position, float orientation)
        {
            var key = $"Ant_{id}";
            if (_elements.TryGetValue(key, out var element))
            {
                var el = element as Path;
                var xAddUpd = -Math.Cos(orientation) * 10;
                var yAddUpd = -Math.Sin(orientation) * 10;
                SetLeft(el!, position.X + xAddUpd);
                SetTop(el!, position.Y + yAddUpd);
                el!.RenderTransform = new RotateTransform((orientation * 180/Math.PI - 75) % 360,0 , 0);
                
                return;
            }
            // center triangle
            var antShape = new Path
            {
                Data = Geometry.Parse("M 0 0 L 5 10 L 10 0 z"),
                Fill = Brushes.Black,
                Stretch = Stretch.Fill,
                Width = 10,
                Height = 10,
                RenderTransform = new RotateTransform((orientation * 180/Math.PI) % 360, 5, 5)
            };
            SetLeft(antShape, position.X);
            SetTop(antShape, position.Y);
            // Z-index causes significant performance degradation
            //SetZIndex(antShape, 2);
            AddElement(key, antShape);
        }

        private SolidColorBrush GetBrush(Color color)
        {
            // If brush exists in dictionary, return it.
            if (_brushes.TryGetValue(color, out var brush))
            {
                return brush;
            }
            // else construct it, place it in the dictionary and return it.
            brush = new SolidColorBrush(color);
            _brushes.Add(color, brush);
            return brush;
        }
        
        public void DrawFood(string key, Point position, float value)
        {
            var color = GetShadeOfBrown(value);
            if (_elements.TryGetValue(key, out var element))
            {
                var el = element as Ellipse;
                el!.Fill = GetBrush(color);
            }
            else
            {
                var foodShape = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = GetBrush(color)
                };
                SetLeft(foodShape, position.X);
                SetTop(foodShape, position.Y);

                AddElement(key, foodShape);
            }
        }

        public void DrawVisionCone(string antKey, Point position, float orientation, int radius, float angle)
        {
            
            var key = $"VisionCone_{antKey}";
            var angleInRad = angle * (float)Math.PI / 180;

            // Calculate the three points defining the circle sector
            double startAngle = orientation - angleInRad / 2;
            double endAngle = orientation + angleInRad / 2;

            // Calculate the two end points of the sector
            Point startPoint = new Point(radius * Math.Cos(startAngle), radius * Math.Sin(startAngle));
            Point endPoint = new Point(radius * Math.Cos(endAngle), radius * Math.Sin(endAngle));
            Point arcPoint = new Point(0, 0);

            // Create the path geometry
            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = startPoint;

            // Add the arc segment
            var isLargeArc = angleInRad > Math.PI;
            pathFigure.Segments.Add(
                new ArcSegment(
                    endPoint,
                    new Size(radius, radius),
                    0,
                    isLargeArc,
                    SweepDirection.Clockwise,
                    true
                )
            );

            // Close the sector
            pathFigure.Segments.Add(new LineSegment(arcPoint, true));
            pathFigure.IsClosed = true;
            pathGeometry.Figures.Add(pathFigure);

            // Create the path and set its fill
            Path path = new Path();
            path.Data = pathGeometry;
            path.Fill = Brushes.LightBlue;
            path.Opacity = 0.15;

            // Create the transform group and add the rotation transform
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new RotateTransform(startAngle + angleInRad / 2, 0, 0));
            transformGroup.Children.Add(new RotateTransform(orientation, 0, 0));
            transformGroup.Children.Add(new TranslateTransform(position.X, position.Y));
            path.RenderTransform = transformGroup;

            if (_elements.ContainsKey(key))
            {
                RemoveElement(key);
            }

            AddElement(key, path);
        }


        public void DrawHome()
        {
            var key = "Home";
            if (!_elements.ContainsKey(key))
            {
                // Draw a black circle in the center of the left half of the canvas
                var homeShape = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Black
                };
                SetLeft(homeShape, Width / 4);
                SetTop(homeShape, Height / 2);

                AddElement(key, homeShape);
            }
        }
        
        public void RemoveResourcesToBeDeleted()
        {
           // Go through concurrent queue and delete
              while (_deleteResourceQueue.TryDequeue(out var key))
              {
                RemoveElement(key);
              }
        }

        public void DrawPheromone(string type, string key, Point position, float value)
        {
            // scaled value is transformed to asymptotically approach 1
            //var scaledValue = Math.Max((float)(1 - 1 / (1 + value)), 0.1f);

            // if type is "pheromone", color in blue, otherwise in red
            var color = type == "pheromone-r"
                ? GetShadeOfPurple(value)
                : GetShadeOfOrange(value);

            if (_elements.TryGetValue(key, out var element))
            {
                var el = element! as Ellipse;
                el!.Fill = GetBrush(color);
            }
            else
            {

                //var color = type == "pheromone"
                //    ? Brushes.DarkBlue
                //    : Brushes.DarkRed;
                var pheromoneShape = new Ellipse()
                {
                    Width = 4,
                    Height = 4,
                    Fill = GetBrush(color),
                };
                SetLeft(pheromoneShape, position.X);
                SetTop(pheromoneShape, position.Y);
                //SetZIndex(pheromoneShape, -1);

                AddElement(key, pheromoneShape);
            }
        }

        private static Color GenerateShade(Color color, float intensityChange)
        {
            intensityChange = MathF.Max(-0.9f, MathF.Min(0.9f, intensityChange));
            float r = color.R;
            float g = color.G;
            float b = color.B;
            
            // an intensityChange of -1 will return white
            // an intensityChange of 1 will return a fully saturated color
            if (intensityChange < 0)
            {
                intensityChange += 1f;
                r = (int)(255 - (255 - r) * intensityChange);
                g = (int)(255 - (255 - g) * intensityChange);
                b = (int)(255 - (255 - b) * intensityChange);
            }
            else
            {
                r = (int)(r * (1 - intensityChange));
                g = (int)(g * (1 - intensityChange));
                b = (int)(b * (1 - intensityChange));
            }

            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        }
        
        private static Color GetShadeOfBrown(float intensity)
        {
            // logarithmically map intensity (0f to inf) to between -1f and 0f
            var logValue = (float)Math.Log(intensity * 5 + 1f) - 1f;
            logValue = (float)Math.Round(logValue, 2);
            return GenerateShade(Colors.SaddleBrown, logValue);
        }

        private static Color GetShadeOfOrange(float intensity)
        {
            // logarithmically map intensity (0f to inf) to between -1f and 0f
            var logValue = (float)Math.Log(intensity * 5 + 1f) - 1f;
            logValue = (float)Math.Round(logValue, 2);
            return GenerateShade(Colors.Orange, logValue);
        }

        private static Color GetShadeOfPurple(float intensity)
        {
            // logarithmically map intensity (0f to inf) to between -1f and 0f
            var logValue = (float)Math.Log(intensity * 5 + 1f) - 1f;
            // round to 2 decimal places
            logValue = (float)Math.Round(logValue, 2);
            return GenerateShade(Colors.Purple, logValue);
        }
    }
}