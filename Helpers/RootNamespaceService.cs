namespace NetworkMonitor.Maui;

public interface IRootNamespaceService
{
    AppTheme GetRequestedTheme();
    Color GetResourceColor(string key);
    string GetAppDataDirectory();
}

public class RootNamespaceService : IRootNamespaceService
{

    public static IServiceProvider GetServiceProvider()
{
    return NetworkMonitorAgent.MauiProgram.ServiceProvider;
}
    public AppTheme GetRequestedTheme()
    {
        return NetworkMonitorAgent.App.Current?.RequestedTheme ?? AppTheme.Light;
    }

    public Color GetResourceColor(string key)
    {
        return NetworkMonitorAgent.ColorResource.GetResourceColor(key);
    }

    public string GetAppDataDirectory()
    {
        return FileSystem.AppDataDirectory;
    }
}

