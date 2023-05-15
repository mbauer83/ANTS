using AntColonySimulation.Implementations;
using NUnit.Framework;

namespace AntColonySimulationTest;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class SimulationCanvasTest
{
    [Test]
    public void TestAddElement()
    {
        // Arrange
        var canvas = new SimulationCanvas(100, 100);
        var element = new System.Windows.Shapes.Rectangle();

        // Act
        canvas.AddElement("test", element);

        // Assert
        Assert.That(canvas.Children.Contains(element));
    }

    [Test]
    public void TestRemoveElement()
    {
        // Arrange
        var canvas = new SimulationCanvas(100, 100);
        var element = new System.Windows.Shapes.Rectangle();
        canvas.AddElement("test", element);

        // Act
        canvas.RemoveElement("test");

        // Assert
        Assert.That(!canvas.Children.Contains(element));
    }

    [Test]
    public void TestClearElements()
    {
        // Arrange
        var canvas = new SimulationCanvas(100, 100);
        var element1 = new System.Windows.Shapes.Rectangle();
        var element2 = new System.Windows.Shapes.Rectangle();
        canvas.AddElement("test1", element1);
        canvas.AddElement("test2", element2);

        // Act
        canvas.ClearElements();

        // Assert
        Assert.That(canvas.Children.Count == 0);
    }

    [Test]
    public void TestDrawAnt()
    {
        // Arrange
        var canvas = new SimulationCanvas(100, 100);

        // Act
        canvas.DrawAnt("test", new System.Windows.Point(50, 50), 0);

        // Assert
        Assert.That(canvas.Children.Count == 1);
    }
}