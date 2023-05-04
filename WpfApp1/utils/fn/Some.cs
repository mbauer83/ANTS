using System;
using System.Collections.Generic;

namespace WpfApp1.utils.fn;

public class Some<T>: IOption<T>
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

    public IMonad<T1> Map<T1>(Func<T, T1> f)
    {
        return new Some<T1>(f(Value));
    }
    
    public IMonad<T1> Apply<T1>(IMonad<Func<T, T1>> f)
    {
        if (f is Some<Func<T, T1>> some)
        {
            return new Some<T1>(some.Get()(Value));
        }

        return new None<T1>();
    }
    
    //public static IOption<T> Pure(T value)
    //{
    //    return new Some<T>(value);
    //}

    public IMonad<T1> Pure<T1>(T1 value)
    {
        return new Some<T1>(value);
    }

    public IMonad<T1> FlatMap<T1>(Func<T, IMonad<T1>> f)
    {
        return f(Value);
    }
    
    public void Match(Action<T> some, Action none)
    {
        some(Value);
    }

    public T1 MatchReturn<T1>(Func<T, T1> some, Func<T1> none)
    {
        return some(Value);
    }
    
}