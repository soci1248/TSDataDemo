namespace TSDataDemo;

public static class Extensions
{
    private static readonly float FloatEpsilon = 0.0001F;

    public static bool IsZero(this float a) => a.LoseEqual(0F);
    public static bool LoseEqual(this float a, float b) => Math.Abs(a - b) < FloatEpsilon;
}