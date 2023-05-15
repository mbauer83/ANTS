using System.Windows;
using ANTS.Implementations;

namespace ANTSTest;

public class FoodResourceTest
{
    private FoodResource _foodResource;

    [SetUp]
    public void Setup()
    {
        _foodResource = new FoodResource(new Point(0, 0), 10f, 0.1f);
    }

    [Test]
    public void Split_ReturnsTwoFoodResources()
    {
        var (resource1, resource2) = _foodResource.Split(5f);

        Assert.IsInstanceOf<FoodResource>(resource1.Get());
        Assert.IsInstanceOf<FoodResource>(resource2.Get());
    }

    [Test]
    public void WithAmount_ReturnsNewFoodResourceWithCorrectAmount()
    {
        var newAmount = 5f;
        var newResource = _foodResource.WithAmount(newAmount);

        Assert.That(newResource.Amount, Is.EqualTo(newAmount));
    }

    [Test]
    public void Decayed_ReturnsNewFoodResourceWithCorrectAmount()
    {
        var deltaTime = 1f;
        var decayRateModifier = 1f;
        var expectedAmount = _foodResource.Amount * (1f - _foodResource.DecayRate * decayRateModifier * deltaTime);
        var newResource = _foodResource.Decayed(deltaTime, decayRateModifier);

        Assert.That(newResource.Amount, Is.EqualTo(expectedAmount));
    }
}
