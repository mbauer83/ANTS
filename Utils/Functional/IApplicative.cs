using System;

namespace AntColonySimulation.Utils.Functional;

public interface IApplicative<T>
{
    IApplicative<T1> Map<T1>(Func<T, T1> f);
    IApplicative<T1> Apply<T1>(IApplicative<Func<T, T1>> f);
    public static abstract IApplicative<T> Pure(T value);
}