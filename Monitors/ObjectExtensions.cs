namespace FluidBar.Monitors;

internal static class ObjectExtensions
{
    public static T Apply<T>(this T value, Action<T> configure)
    {
        configure(value);
        return value;
    }
}