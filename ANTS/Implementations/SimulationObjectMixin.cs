namespace ANTS.Implementations;

public interface SimulationObjectMixin
{
    public static string KeyFor(string type, float x, float y)
    {
        var xTo4DecimalPlaces = x.ToString("0.0000");
        var yTo4DecimalPlaces = y.ToString("0.0000");
        return $"({xTo4DecimalPlaces},{yTo4DecimalPlaces})-{type}";
    }
}