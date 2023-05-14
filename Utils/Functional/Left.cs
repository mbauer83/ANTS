using System;

namespace AntColonySimulation.Utils.Functional;

public class Left<TLeft, TRight>: IEither<TLeft, TRight> where TLeft: Exception
{
    public readonly TLeft Value;
    
    public Left(TLeft value)
    {
        Value = value;
    }
    
    public bool IsLeft()
    {
        return true;
    }
    
    public bool IsRight()
    {
        return false;
    }
    
    public IEither<TLeft1, TRight1> MapEither<TLeft1, TRight1>(Func<TLeft1> e, Func<TRight, TRight1> f)
        where TLeft1 : Exception
    {
        return new Left<TLeft1, TRight1>(e());
    }
    
    public IMonad<T2> Map<T2>(Func<TRight, T2> f)
    {
        return new Left<TLeft, T2>(Value);
    }
    
    public IMonad<T2> Apply<T2>(IMonad<Func<TRight, T2>> f)
    {
        return new Left<TLeft, T2>(Value);
    }
    
    public IMonad<TRight1> Pure<TRight1>(TRight1 value)
    {
        return new Right<TLeft, TRight1>(value);
    }
    
    public IMonad<T2> FlatMap<T2>(Func<TRight, IMonad<T2>> f)
    {
        return new Left<TLeft, T2>(Value);
    }
}