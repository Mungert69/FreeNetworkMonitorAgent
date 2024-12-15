using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Maui.Controls;

namespace NetworkMonitor.Maui;

public class RootNamespaceService
{
    // Dynamically resolve MainActivity Type using reflection
    public static Type MainActivity
    {
        get
        {
            try
            {
#if ANDROID
                var appNamespace = Application.Current?.GetType().Namespace;
                if (!string.IsNullOrEmpty(appNamespace))
                {
                    var mainActivityType = Type.GetType($"{appNamespace}.MainActivity");
                    if (mainActivityType != null)
                        return mainActivityType;
                }
                throw new InvalidOperationException("Unable to resolve MainActivity. Ensure the namespace is correct.");
#else
                throw new PlatformNotSupportedException("MainActivity is only available on Android.");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving MainActivity: {ex.Message}");
                return null;
            }
        }
    }

    // Dynamically resolve ServiceProvider via MauiProgram
    public static IServiceProvider ServiceProvider
    {
        get
        {
            try
            {
                // Locate the MauiProgram type dynamically
                var appNamespace = Application.Current?.GetType().Namespace;
                if (!string.IsNullOrEmpty(appNamespace))
                {
                    var mauiProgramType = Type.GetType($"{appNamespace}.MauiProgram");
                    if (mauiProgramType != null)
                    {
                        var serviceProviderProperty = mauiProgramType.GetProperty("ServiceProvider", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (serviceProviderProperty?.GetValue(null) is IServiceProvider serviceProvider)
                            return serviceProvider;
                    }
                }
                throw new InvalidOperationException("Unable to resolve ServiceProvider. Ensure MauiProgram contains a public static ServiceProvider property.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving ServiceProvider: {ex.Message}");
                return null;
            }
        }
    }

    public string GetAppDataDirectory()
    {
        return FileSystem.AppDataDirectory;
    }
}
