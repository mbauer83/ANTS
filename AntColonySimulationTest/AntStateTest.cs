using AntColonySimulation.Implementations;
using NUnit.Framework;

namespace AntColonySimulationTest;

public class AntStateTests
{
    [Test]
    public void WithData_ReturnsNewInstanceWithCorrectlyUpdatedData()
    {
        // Arrange
        var antState = new AntState(0, 0, 0, 0, 0);

        // Act
        var updatedAntState = antState.WithData(x: 1, y: 2, orientation: 3, speed: 4, totalFoodCarried: 5, sensoryFieldAngle: 6, sensoryFieldRadius: 7);
        Assert.Multiple(() =>
        {

            // Assert
            Assert.That(updatedAntState.X, Is.EqualTo(1));
            Assert.That(updatedAntState.Y, Is.EqualTo(2));
            Assert.That(updatedAntState.Orientation, Is.EqualTo(3));
            Assert.That(updatedAntState.Speed, Is.EqualTo(4));
            Assert.That(updatedAntState.TotalFoodCarried, Is.EqualTo(5));
            Assert.That(updatedAntState.SensoryFieldAngle, Is.EqualTo(6));
            Assert.That(updatedAntState.SensoryFieldRadius, Is.EqualTo(7));
        });
    }
}
