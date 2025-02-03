#if ANDROID
using Android.Content.Res;
using Android.App;
using Android.Graphics;
using AndroidX.Core.Content;
#endif

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics; // Ensure you are using the correct namespace for Color
using NetworkMonitor.Maui.Controls;

namespace NetworkMonitorAgent
{
    public class ColorResource : IColorResource
    {
#if ANDROID
        public AppTheme GetRequestedTheme()
        {
            try
            {
                var uiModeFlags = Android.App.Application.Context.Resources.Configuration.UiMode;
                return (uiModeFlags & UiMode.NightMask) == UiMode.NightYes ? AppTheme.Dark : AppTheme.Light;
            }
            catch
            {
                // Add some logging or handling here if needed
            }

            return AppTheme.Light;
        }

        public Microsoft.Maui.Graphics.Color GetResourceColor(string key)  // Use Microsoft.Maui.Graphics.Color for consistency
        {
            try
            {
                int resourceId = Android.App.Application.Context.Resources.GetIdentifier(key, "color", Android.App.Application.Context.PackageName);
                if (resourceId != 0)
                {
                    int androidColor = ContextCompat.GetColor(Android.App.Application.Context, resourceId);
                    return Microsoft.Maui.Graphics.Color.FromUint((uint)androidColor); // Convert to Maui Color
                }
            }
            catch
            {
                // Add some logging or handling here if needed
            }

           return new Microsoft.Maui.Graphics.Color(0, 0, 0, 0); // Transparent color
       
        }

#else
        public AppTheme GetRequestedTheme()
        {
            try
            {
                if (Microsoft.Maui.Controls.Application.Current != null && Microsoft.Maui.Controls.Application.Current.RequestedTheme != null)
                {
                    return Microsoft.Maui.Controls.Application.Current.RequestedTheme;
                }
            }
            catch
            {
                // Add some logging or handling here if needed
            }

            return AppTheme.Light;
        }

        public Microsoft.Maui.Graphics.Color GetResourceColor(string key)
        {
            try
            {
                if (Microsoft.Maui.Controls.Application.Current != null && Microsoft.Maui.Controls.Application.Current.Resources.TryGetValue(key, out var colorValue))
                {
                    return (Microsoft.Maui.Graphics.Color)colorValue; // Use Microsoft.Maui.Graphics.Color
                }
            }
            catch
            {
                // Add some logging or handling here if needed
            }

            return new Microsoft.Maui.Graphics.Color(0, 0, 0, 0);
        }

#endif

        public Microsoft.Maui.Graphics.Color LightenColor(Microsoft.Maui.Graphics.Color color, float factor)
        {
            factor = Math.Max(0, factor); // Ensure factor is non-negative

            return new Microsoft.Maui.Graphics.Color(
                Math.Min(color.Red + factor, 1.0f),
                Math.Min(color.Green + factor, 1.0f),
                Math.Min(color.Blue + factor, 1.0f));
        }

        public void AnimateColor(BoxView boxView, Microsoft.Maui.Graphics.Color fromColor, Microsoft.Maui.Graphics.Color toColor, uint length)
        {
            if (boxView == null) throw new ArgumentNullException(nameof(boxView));
            if (fromColor == null) throw new ArgumentNullException(nameof(fromColor));
            if (toColor == null) throw new ArgumentNullException(nameof(toColor));

            var animation = new Animation(v =>
            {
                var color = Microsoft.Maui.Graphics.Color.FromRgb(
                    lerp(fromColor.Red, toColor.Red, v),
                    lerp(fromColor.Green, toColor.Green, v),
                    lerp(fromColor.Blue, toColor.Blue, v));
                boxView.Color = color;
            }, 0, 1);

            animation.Commit(boxView, "ColorChange", length: length);
        }

        private double lerp(double start, double end, double t)
        {
            return start + (end - start) * t;
        }
    }
}
