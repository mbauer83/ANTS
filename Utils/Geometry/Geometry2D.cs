using System;

namespace AntColonySimulation.Utils.Geometry;

public class Geometry2D
{
    public static float AbsDistance(float x1, float y1, float x2, float y2)
    {
        return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
    }

    public static float EuclideanDistance(float x1, float y1, float x2, float y2)
    {
        return (float)Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
    }

    public static float AngleBetween(float x1, float y1, float x2, float y2)
    {
        return (float)Math.Atan2(y2 - y1, x2 - x1);
    }
}