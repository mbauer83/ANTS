using System;

namespace AntColonySimulation.Utils.Functional;

public interface IEither<out TLeft, out TRight> : IMonad<TRight> where TLeft : Exception
{
    public bool IsLeft();
    public bool IsRight();

    public IEither<TLeft1, TRight1> MapEither<TLeft1, TRight1>(Func<TLeft1> e, Func<TRight, TRight1> f)
        where TLeft1 : Exception;
    //public static abstract IEither<TLeft, TRight> Pure(TRight value);
}