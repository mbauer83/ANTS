using ANTS.Utils.Functional;

namespace ANTS.Implementations;

public readonly struct SplittableAmount
{
    public float Amount { get; }

    public SplittableAmount(float amount)
    {
        Amount = amount;
    }

    public (IOption<SplittableAmount>, IOption<SplittableAmount>) Split(float firstPartAmount)
    {
        if (firstPartAmount >= Amount) return (new Some<SplittableAmount>(this), new None<SplittableAmount>());

        if (firstPartAmount <= 0) return (new None<SplittableAmount>(), new Some<SplittableAmount>(this));
        var firstPart = new SplittableAmount(firstPartAmount);
        var secondPart = new SplittableAmount(Amount - firstPartAmount);
        return (new Some<SplittableAmount>(firstPart), new Some<SplittableAmount>(secondPart));
    }
}