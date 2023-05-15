using System;

namespace ANTS.Utils.Functional;

public interface IOption<T> : IMonad<T>
{
    public bool IsSome();
    public bool IsNone();

    /**
     * @throws InvalidOperationException if IsNone() is true
     */
    public T Get();

    public T GetOrElse(T defaultValue);

    // Context parameter available to avoid closure allocation in lambdas
    public void Match<T1>(Action<(T, T1)> some, Action<T1> none, T1 context);
    public T1 MatchReturn<T1, T2>(Func<(T, T2), T1> some, Func<T2, T1> none, T2 context);

    public static IOption<T1> FromNullable<T1>(T1? value)
    {
        return null == value ? new None<T1>() : new Some<T1>(value);
    }
}