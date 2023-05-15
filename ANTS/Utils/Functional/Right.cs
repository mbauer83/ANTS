using System;

namespace ANTS.Utils.Functional;

public class Right<TLeft, TRight> : IEither<TLeft, TRight> where TLeft : Exception
{
    public readonly TRight Value;

    public Right(TRight value)
    {
        Value = value;
    }

    public bool IsLeft()
    {
        return false;
    }

    public bool IsRight()
    {
        return true;
    }

    public IEither<TLeft1, TRight1> MapEither<TLeft1, TRight1>(Func<TLeft1> e, Func<TRight, TRight1> f)
        where TLeft1 : Exception
    {
        try
        {
            var res = f(Value);
            return new Right<TLeft1, TRight1>(res);
        }
        catch (TLeft1 ex)
        {
            return new Left<TLeft1, TRight1>(ex);
        }
    }

    public IMonad<TRight1> Map<TRight1>(Func<TRight, TRight1> f)
    {
        return new Right<TLeft, TRight1>(f(Value));
    }

    public IMonad<TRight1> Apply<TRight1>(IMonad<Func<TRight, TRight1>> f)
    {
        return f.Map(f1 => f1(Value));
    }

    public IMonad<TRight1> Pure<TRight1>(TRight1 value)
    {
        return new Right<TLeft, TRight1>(value);
    }

    public IMonad<TRight1> FlatMap<TRight1>(Func<TRight, IMonad<TRight1>> f)
    {
        return f(Value);
    }
}