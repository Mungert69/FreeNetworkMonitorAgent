
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui.Controls;
using NetworkMonitor.Objects;

namespace NetworkMonitorAgent
{
    public class RootNamespaceProvider : IRootNamespaceProvider
    {
        private static bool _assetsReady = false;

        public static bool AssetsReady { get => _assetsReady; set => _assetsReady = value; }
        public IServiceProvider ServiceProvider { get => MauiProgram.ServiceProvider; }
        public string GetAppDataDirectory() => FileSystem.AppDataDirectory;
        public IColorResource ColorResource => new ColorResource();


#if ANDROID
        public Type MainActivity { get => typeof(MainActivity); }
           // Method to retrieve a Drawable field dynamically (Optional based on use case)
       public int GetDrawable(string drawableName)
{
    try
    {
        int resourceId;
        switch (drawableName?.ToLowerInvariant()) // Case-insensitive match
        {
            case "logo":
                resourceId = Resource.Drawable.logo;
                break;
            case "view":
                resourceId = Resource.Drawable.view;
                break;
            case "stop":
                resourceId = Resource.Drawable.stop;
                break;
            default:
                // Fallback to a system icon
                resourceId = Android.Resource.Drawable.IcDialogAlert;
                Console.WriteLine($"Drawable '{drawableName}' not found; using system fallback icon.");
                break;
        }

        // Validate the resource ID
        if (IsValidResourceId(resourceId))
        {
            return resourceId;
        }
        else
        {
            Console.WriteLine($"Invalid resource ID for drawable '{drawableName}'; using system fallback icon.");
            return Android.Resource.Drawable.IcDialogAlert;
        }
    }
    catch (Exception ex)
    {
        // Log the exception and return the default system icon
        Console.WriteLine($"Error retrieving drawable '{drawableName}': {ex.Message}");
        return Android.Resource.Drawable.IcDialogAlert;
    }
}

private bool IsValidResourceId(int resourceId)
{
    // Resource IDs in Android are positive integers
    return resourceId > 0;
}
#else
        public Type MainActivity { get => typeof(object); }

        public int GetDrawable(string drawableName) => 0;
        
#endif

    }
}
