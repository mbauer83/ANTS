using System;

namespace AntColonySimulation.Utils.Functional;

public interface IFunctor<T>
{
    abstract IFunctor<T1> Map<T1>(Func<T, T1> f);
}