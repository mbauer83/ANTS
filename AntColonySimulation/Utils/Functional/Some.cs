using System;

namespace AntColonySimulation.Utils.Functional;

public class Some<T> : IOption<T>
{
    public readonly T Value;

    public Some(T value)
    {
        Value = value;
    }

    public bool IsSome()
    {
        return true;
    }

    public bool IsNone()
    {
        return false;
    }

    public T Get()
    {
        return Value;
    }

    public T GetOrElse(T defaultValue)
    {
        return Value;
    }

    public IMonad<T1> Map<T1>(Func<T, T1> f)
    {
        return new Some<T1>(f(Value));
    }

    public IMonad<T1> Apply<T1>(IMonad<Func<T, T1>> f)
    {
        if (f is Some<Func<T, T1>> some) return new Some<T1>(some.Get()(Value));

        return new None<T1>();
    }

    public IMonad<T1> Pure<T1>(T1 value)
    {
        return new Some<T1>(value);
    }

    public IMonad<T1> FlatMap<T1>(Func<T, IMonad<T1>> f)
    {
        return f(Value);
    }

    public void Match<T1>(Action<(T, T1)> some, Action<T1> none, T1 context)
    {
        some((Value, context));
    }

    public T1 MatchReturn<T1, T2>(Func<(T, T2), T1> some, Func<T2, T1> none, T2 context)
    {
        return some((Value, context));
    }
}