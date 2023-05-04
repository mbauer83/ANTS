using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp1
{
    public class SimulationCanvas : Canvas
    {
        private readonly Dictionary<string, UIElement> _elements = new Dictionary<string, UIElement>();

        public SimulationCanvas(int width, int height)
        {
            Background = Brushes.White;
            // set size
            Width = width;
            Height = height;
        }

        public void AddElement(string name, UIElement element)
        {
            _elements.Add(name, element);
            Children.Add(element);
        }

        public void RemoveElement(string name)
        {
            if (_elements.TryGetValue(name, out UIElement element))
            {
                Children.Remove(element);
                _elements.Remove(name);
            }
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
                SetLeft(el, position.X + xAddUpd);
                SetTop(el, position.Y + yAddUpd);
                el.RenderTransform = new RotateTransform((orientation * 180/Math.PI - 75) % 360,0 , 0);
                
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
            var xAdd = 0;//-Math.Cos(orientation) * 5;
            var yAdd = 0;//-Math.Sin(orientation) * 5;
            SetLeft(antShape, position.X + xAdd);
            SetTop(antShape, position.Y + yAdd);
            AddElement(key, antShape);
        }

        public void DrawFood(string key, Point position, float value)
        {
            // Light green to dark green from 0 to 1
            var lightGreenRgb = new byte[] { 144, 238, 144 };
            var darkGreenRgb = new byte[] { 0, 100, 0 };
            var color = Color.FromRgb(
                (byte)(lightGreenRgb[0] + (darkGreenRgb[0] - lightGreenRgb[0]) * value),
                (byte)(lightGreenRgb[1] + (darkGreenRgb[1] - lightGreenRgb[1]) * value),
                (byte)(lightGreenRgb[2] + (darkGreenRgb[2] - lightGreenRgb[2]) * value)
            );
            if (_elements.TryGetValue(key, out var element))
            {
                var el = element as Ellipse;
                el.Fill = new SolidColorBrush(color);
            }
            else
            {
                var foodShape = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = new SolidColorBrush(color)
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
            path.Opacity = 0.25;

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

        public void DrawPheromone(string type, string key, Point position, float value)
        {
            // scaled value is transformed to asymptotically approach 1
            var scaledValue = Math.Max((float)(1 - 1 / (1 + value)), 0.1f);

            // if type is "pheromone", color in blue, otherwise in red
            var color = type == "pheromone-r"
                ? Color.FromRgb((byte)(255 * scaledValue), (byte)(255 * scaledValue), 255)
                : Color.FromRgb(255, (byte)(255 * scaledValue), (byte)(255 * scaledValue));
            
            if (_elements.TryGetValue(key, out var element))
            {
                var el = element as Ellipse;
                el.Fill = new SolidColorBrush(color);
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
                    Fill = new SolidColorBrush(color),
                };
                SetLeft(pheromoneShape, position.X);
                SetTop(pheromoneShape, position.Y);

                AddElement(key, pheromoneShape);
            }
        }
    }
}