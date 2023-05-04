using System;
using System.Windows.Media.Media3D;

namespace WpfApp1.utils.fn;

public interface IMonad<T>
{
    IMonad<T1> Map<T1>(Func<T, T1> f);
    IMonad<T1> Apply<T1>(IMonad<Func<T, T1>> f);
    IMonad<T1> Pure<T1>(T1 value);
    public IMonad<T1> FlatMap<T1>(Func<T, IMonad<T1>> f);
}