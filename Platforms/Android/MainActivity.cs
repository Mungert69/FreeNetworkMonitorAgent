using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace NetworkMonitorAgent;

    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // This makes the window adjust when keyboard appears
        Window.SetSoftInputMode(SoftInput.AdjustResize);
        
        // Optional: If you want more control over the keyboard behavior
        // Window.SetSoftInputMode(SoftInput.AdjustPan | SoftInput.StateHidden);
    }
}