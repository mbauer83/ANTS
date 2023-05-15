using System.Windows;
using ANTS.Implementations;

namespace ANTSTest;

public class PheromoneResourceReturnTest
{
    private PheromoneResourceReturn _PheromoneResourceReturn;

    [SetUp]
    public void Setup()
    {
        _PheromoneResourceReturn = new PheromoneResourceReturn(new Point(0, 0), 10f, 0.1f);
    }

    [Test]
    public void Split_ReturnsTwoFoodResources()
    {
        var (resource1, resource2) = _PheromoneResourceReturn.Split(5f);

        Assert.IsInstanceOf<PheromoneResourceReturn>(resource1.Get());
        Assert.IsInstanceOf<PheromoneResourceReturn>(resource2.Get());
    }

    [Test]
    public void WithAmount_ReturnsNewPheromoneResourceReturnWithCorrectAmount()
    {
        var newAmount = 5f;
        var newResource = _PheromoneResourceReturn.WithAmount(newAmount);

        Assert.That(newResource.Amount, Is.EqualTo(newAmount));
    }

    [Test]
    public void Decayed_ReturnsNewPheromoneResourceReturnWithCorrectAmount()
    {
        var deltaTime = 1f;
        var decayRateModifier = 1f;
        var expectedAmount = _PheromoneResourceReturn.Amount * (1f - _PheromoneResourceReturn.DecayRate * decayRateModifier * deltaTime);
        var newResource = _PheromoneResourceReturn.Decayed(deltaTime, decayRateModifier);

        Assert.That(newResource.Amount, Is.EqualTo(expectedAmount));
    }
}
