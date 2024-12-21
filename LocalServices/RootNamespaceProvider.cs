
using NetworkMonitor.Maui.Services;

namespace NetworkMonitorAgent
{
    public class RootNamespaceProvider : IRootNamespaceProvider
    {
        private static bool _assetsReady = false;

        public static bool AssetsReady { get => _assetsReady; set => _assetsReady = value; }
        public IServiceProvider ServiceProvider { get => MauiProgram.ServiceProvider; }
        public string GetAppDataDirectory() => FileSystem.AppDataDirectory;


#if ANDROID
        public Type MainActivity { get => typeof(MainActivity); }
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
#else
        public Type MainActivity { get => typeof(object); }

        public int GetDrawable(string drawableName) => 0;
        
#endif

    }
}
