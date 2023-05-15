using System;

namespace ANTS.Utils.Functional;

public interface IFunctor<out T>
{
    IFunctor<T1> Map<T1>(Func<T, T1> f);
}