using ANTS.Implementations;
using ANTS.Utils.Functional;

namespace ANTSTest;

public class SplittableAmountTest
{
    [Test]
    public void TestSplitSplitsCorrectly()
    {
        // Create SplittableAmount with amount 10
        var splittableAmount = new SplittableAmount(10);
        // Split it into two parts, first part with amount 3
        var (firstPart, secondPart) = splittableAmount.Split(3);
        Assert.Multiple(() =>
        {
            // Check that none of the parts are None
            Assert.That(firstPart is None<SplittableAmount>, Is.False);
            Assert.That(secondPart is None<SplittableAmount>, Is.False);
            // Check that parts have correct amounts
            Assert.That(firstPart.Get().Amount, Is.EqualTo(3));
            Assert.That(secondPart.Get().Amount, Is.EqualTo(7));
        });
    }
    
    [Test]
    public void TestSecondSplitResultIsNoneIfSplitAmountIsTooBig()
    {
        // Create SplittableAmount with amount 10
        var splittableAmount = new SplittableAmount(10);
        // Split it into two parts, first part with amount 11
        var (firstPart, secondPart) = splittableAmount.Split(11);
        Assert.Multiple(() =>
        {
            // Check that first part is None
            Assert.That(firstPart is None<SplittableAmount>, Is.False);
            // Check that second part is not None
            Assert.That(secondPart is None<SplittableAmount>, Is.True);
            // Check that second part has correct amount
            Assert.That(firstPart.Get().Amount, Is.EqualTo(10));
        });
    }
}