using System;

namespace WpfApp1.utils.fn;

public interface IFunctor<T>
{
    abstract IFunctor<T1> Map<T1>(Func<T, T1> f);
}