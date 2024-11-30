namespace NetworkMonitorAgent;
public static class ColorResource
{
    public static Color GetResourceColor(string key)
    {
        if (Application.Current!=null && Application.Current.Resources.TryGetValue(key, out var colorValue))
        {
            return (Color)colorValue;
        }
        return Colors.Transparent; // Fallback color if the resource is not found
    }

    public static Color LightenColor(Color color, float factor)
{
    return new Color(
        Math.Min(color.Red + factor, 1.0f),
        Math.Min(color.Green + factor, 1.0f),
        Math.Min(color.Blue + factor, 1.0f));
}

public static void AnimateColor(BoxView boxView, Color fromColor, Color toColor, uint length)
{
    var animation = new Animation(v =>
    {
        var color = Color.FromRgb(
            lerp(fromColor.Red, toColor.Red, v),
            lerp(fromColor.Green, toColor.Green, v),
            lerp(fromColor.Blue, toColor.Blue, v));
        boxView.Color = color;
    }, 0, 1);

    animation.Commit(boxView, "ColorChange", length: length);
}

private static double lerp(double start, double end, double t)
{
    return start + (end - start) * t;
}




}
