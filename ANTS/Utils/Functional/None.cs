using System;

namespace ANTS.Utils.Functional;

public class None<T> : IOption<T>
{
    public bool IsSome()
    {
        return false;
    }

    public bool IsNone()
    {
        return true;
    }

    /**
     * @throws InvalidOperationException if IsNone() is true
     */
    public T Get()
    {
        throw new InvalidOperationException("Cannot get value from None.");
    }

    public T GetOrElse(T defaultValue)
    {
        return defaultValue;
    }

    public IMonad<T1> Map<T1>(Func<T, T1> f)
    {
        return new None<T1>();
    }

    public IMonad<T1> Apply<T1>(IMonad<Func<T, T1>> f)
    {
        return new None<T1>();
    }

    public IMonad<T1> Pure<T1>(T1 value)
    {
        return new Some<T1>(value);
    }

    public IMonad<T1> FlatMap<T1>(Func<T, IMonad<T1>> f)
    {
        return new None<T1>();
    }

    public void Match<T1>(Action<(T, T1)> some, Action<T1> none, T1 context)
    {
        none(context);
    }

    public T1 MatchReturn<T1, T2>(Func<(T, T2), T1> some, Func<T2, T1> none, T2 context)
    {
        return none(context);
    }
}