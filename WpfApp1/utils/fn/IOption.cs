using System;

namespace WpfApp1.utils.fn;

public interface IOption<T>: IMonad<T>
{
    public bool IsSome();
    public bool IsNone();
    
    /**
     * @throws InvalidOperationException if IsNone() is true
     */
    public T Get();
    //public static abstract IOption<T> Pure(T value);
    
    public void Match(Action<T> some, Action none);
    public T1 MatchReturn<T1>(Func<T, T1> some, Func<T1> none);

}