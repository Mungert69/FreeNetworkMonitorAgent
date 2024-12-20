#if ANDROID
using NetworkMonitor.Maui.Services;

namespace NetworkMonitorAgent
{
    public class RootNamespaceProvider : IRootNamespaceProvider
    {

        public Type MainActivity { get => typeof(MainActivity); }
     
public IServiceProvider ServiceProvider { get => MauiProgram.ServiceProvider; }

        // Removed duplicate 'Resource' property; accessing Drawable via GetDrawable method instead.
        public string GetAppDataDirectory() => FileSystem.AppDataDirectory;

        // Method to retrieve a Drawable field dynamically (Optional based on use case)
        public int GetDrawable(string drawableName)
        {
            return drawableName switch
            {
                "logo" => Resource.Drawable.logo,
                "view" => Resource.Drawable.view,
                "stop" => Resource.Drawable.stop,
                _ => Resource.Drawable.logo
            };
        }

    }
}
#endif