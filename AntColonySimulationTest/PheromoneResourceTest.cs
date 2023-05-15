using System.Windows;
using AntColonySimulation.Implementations;

namespace AntColonySimulationTest;

public class PheromoneResourceTest
{
    private PheromoneResource _pheromoneResource;

    [SetUp]
    public void Setup()
    {
        _pheromoneResource = new PheromoneResource(new Point(0, 0), 10f, 0.1f);
    }

    [Test]
    public void Split_ReturnsTwoFoodResources()
    {
        var (resource1, resource2) = _pheromoneResource.Split(5f);

        Assert.IsInstanceOf<PheromoneResource>(resource1.Get());
        Assert.IsInstanceOf<PheromoneResource>(resource2.Get());
    }

    [Test]
    public void WithAmount_ReturnsNewPheromoneResourceWithCorrectAmount()
    {
        var newAmount = 5f;
        var newResource = _pheromoneResource.WithAmount(newAmount);

        Assert.That(newResource.Amount, Is.EqualTo(newAmount));
    }

    [Test]
    public void Decayed_ReturnsNewPheromoneResourceWithCorrectAmount()
    {
        var deltaTime = 1f;
        var decayRateModifier = 1f;
        var expectedAmount = _pheromoneResource.Amount * (1f - _pheromoneResource.DecayRate * decayRateModifier * deltaTime);
        var newResource = _pheromoneResource.Decayed(deltaTime, decayRateModifier);

        Assert.That(newResource.Amount, Is.EqualTo(expectedAmount));
    }
}
