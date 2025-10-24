using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Connection;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Api.Services;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui;
using NetworkMonitor.Security;
using NetworkMonitor.Maui.Helpers;
using NetworkMonitor.Objects;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Utils.Helpers;
using NetworkMonitorChat;
using CommunityToolkit.Maui;
using Microsoft.JSInterop;
using System.Xml;
using System.Text.Json;
using System.IO;

namespace NetworkMonitorAgent
{
    public static class MauiProgram
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static MauiApp CreateMauiApp()
        {
            // Global exception handlers
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {
                    ExceptionHelper.HandleGlobalException(exception, "Unhandled Domain Exception");
                }
            };
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                e.SetObserved(); // Prevent the process from terminating
                ExceptionHelper.HandleGlobalException(e.Exception, "Unobserved Task Exception");
            };
            string os = "";
            ServiceInitializer.Initialize(new RootNamespaceProvider());
#if ANDROID
			    os="android";
#endif
#if WINDOWS
            os = "windows";
#endif
            MauiAppBuilder builder = CreateBuilder();
            try
            {
                builder.Services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders(); // Optional: Clears default providers if necessary
                    loggingBuilder.SetMinimumLevel(LogLevel.Debug); // Set the minimum log level
                    loggingBuilder.AddConsole(); // Console logger (useful for debugging)
                    loggingBuilder.AddDebug();   // Debug output window (useful in Visual Studio)

                    // You can add other logging providers here, such as:
                    // loggingBuilder.AddEventLog(); // Windows Event Log
                    // loggingBuilder.AddFile("app.log"); // File-based logging (requires additional package)
                });
            }
            catch (Exception ex)
            {
                ExceptionHelper.HandleGlobalException(ex, "Error: could not setup logging");
            }

            try
            {
                var fullAppName = AppInfo.Current.Name;
                MauiProgramHelper.LoadConfiguration(builder, fullAppName);
                LoadAssets(builder, os);
                BuildRepoAndConfig(builder);
                BuildServices(builder);
                BuildViewModels(builder);
                BuildPages(builder);
            }
            catch (Exception ex)
            {
                ExceptionHelper.HandleGlobalException(ex, "Initialization Error");
            }
            try
            {
                builder.Services.AddSingleton<AppShell>(provider =>
                        {
                            var logger = provider.GetRequiredService<ILogger<AppShell>>();
                            var platformService = provider.GetRequiredService<IPlatformService>();
                            return new AppShell(logger, platformService);
                        });
            }
            catch (Exception ex)
            {
                ExceptionHelper.HandleGlobalException(ex, "Error creating AppShell");
            }
            var app = builder.Build();
            ServiceProvider = app.Services;
            return app;
        }

        private static void LoadAssets(MauiAppBuilder builder, string os)
        {
            try
            {
                var config = builder.Configuration;
                if (config != null)
                    Task.Run(async () =>
                    {
                        string output = "";
                        string opensslVersion = config["OpensslVersion"];
                        string versionStr = opensslVersion;
                        if (!string.IsNullOrEmpty(os)) versionStr = $"{opensslVersion}-{os}";
                        output = await CopyAssetsHelper.CopyAssetsToLocalStorage(versionStr, "cs-assets", "dlls");
                        RootNamespaceProvider.AssetsReady = true;
                    });
                else ExceptionHelper.HandleGlobalException(new Exception(), "Config is null");
            }
            catch (Exception ex)
            {
                ExceptionHelper.HandleGlobalException(ex, " Error could not load assets");
            }
        }
        private static MauiAppBuilder CreateBuilder()
        {
            try
            {
                MauiAppBuilder builder = MauiApp.CreateBuilder();
                builder
                    .UseMauiApp<App>()
                    .UseMauiCommunityToolkit()
                    .ConfigureFonts(fonts =>
                    {
                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    });

                builder.Services.AddMauiBlazorWebView();


#if DEBUG
                builder.Services.AddBlazorWebViewDeveloperTools();
#endif
                return builder;
            }
            catch (Exception ex)
            {
                ExceptionHelper.HandleGlobalException(ex, "Error: Could not create builder");
                throw new InvalidOperationException("Failed to initialize MauiAppBuilder.", ex);
            }
        }
        private static void BuildRepoAndConfig(MauiAppBuilder builder)
        {
            string appDataDirectory = FileSystem.AppDataDirectory;
            builder.Services.AddSingleton<IFileRepo>(provider =>
            {
                try
                {
                    bool isRunningOnMauiAndroid = true;
                    var fileRepo = new FileRepo(isRunningOnMauiAndroid, appDataDirectory);
                    return fileRepo;
                }
                catch (Exception ex)
                {
                    ExceptionHelper.HandleGlobalException(ex, "Error : initializing FileRepo");
                    return new FileRepo();
                }
            });
            builder.Services.AddSingleton<IEnvironmentStore>(provider =>
           {
               var envPath = Path.Combine(appDataDirectory, ".env");
               var logger = provider.GetRequiredService<ILogger<EnvFileStore>>();
               var envStore = new EnvFileStore(envPath, logger);
               envStore.LoadIntoProcess();
               return envStore;
           });
            builder.Services.AddSingleton<IProtectedConfigManager>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var envStore = provider.GetRequiredService<IEnvironmentStore>();
                var fileRepo = provider.GetRequiredService<IFileRepo>();
                var logger = provider.GetRequiredService<ILogger<ProtectedConfigManager>>();
                return new ProtectedConfigManager(configuration, envStore, fileRepo, logger);
            });
            builder.Services.AddSingleton<NetConnectConfig>(provider =>
            {
                // Assuming Configuration is properly set up
                var configuration = provider.GetRequiredService<IConfiguration>();
                // Ensure environment variables from .env are loaded before building NetConnectConfig.
                _ = provider.GetRequiredService<IEnvironmentStore>();
                var protectedConfigManager = provider.GetRequiredService<IProtectedConfigManager>();
                string nativeLibDir = string.Empty;
#if ANDROID
                nativeLibDir = Android.App.Application.Context.ApplicationInfo.NativeLibraryDir; 
#endif
                var netConfig = new NetConnectConfig(configuration, appDataDirectory, nativeLibDir);
#if ANDROID
                try
                {
                    netConfig.LocalSystemUrl.AndroidVersion = global::Android.OS.Build.VERSION.Release ?? string.Empty;
                    netConfig.LocalSystemUrl.AndroidSdkLevel = (int)global::Android.OS.Build.VERSION.SdkInt;
                    const string legacyRootCertFile = "isrgrootx1.cer";
                    var rootCertPath = Path.Combine(appDataDirectory, legacyRootCertFile);
                    netConfig.LocalSystemUrl.LegacyAndroidRootCertPath = rootCertPath;
                    if (!File.Exists(rootCertPath))
                    {
                        using var assetStream = FileSystem.OpenAppPackageFileAsync(legacyRootCertFile).GetAwaiter().GetResult();
                        using var destination = File.Create(rootCertPath);
                        assetStream.CopyTo(destination);
                    }
                }
                catch (Exception ex)
                {
                    var logger = provider.GetService<ILogger<NetConnectConfig>>();
                    logger?.LogWarning(ex, "Failed to populate Android version information.");
                }
#endif
                protectedConfigManager
                    .SynchronizeSensitiveValuesAsync(netConfig, ProtectedConfigurationParameters.All)
                    .GetAwaiter()
                    .GetResult();
                return netConfig;
            });
            builder.Services.AddSingleton<LocalProcessorStates>(provider =>
            {
                return new LocalProcessorStates();
            });

            builder.Services.AddSingleton<IRabbitRepo>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<RabbitRepo>>();
                    var netConfig = provider.GetRequiredService<NetConnectConfig>();
                    // Choose the appropriate constructor
                    return new RabbitRepo(logger, netConfig);
                });
        }
        private static void BuildServices(MauiAppBuilder builder)
        {

            builder.Services.AddSingleton<ILaunchHelper, LaunchHelper>();
            builder.Services.AddSingleton<IDialogService, DialogService>();

            builder.Services.AddScoped<IBrowserHost>(provider =>
            {
                var launchHelper = provider.GetRequiredService<ILaunchHelper>();
                var logger = provider.GetRequiredService<ILogger<BrowserHost>>();
                var netConfig = provider.GetRequiredService<NetConnectConfig>();

                return new BrowserHost(launchHelper, netConfig, logger, maxConcurrentPages: 1);
            });

            builder.Services.AddScoped<ILLMService>(provider =>
            {
                return new LLMService(
                    provider.GetRequiredService<NetConnectConfig>());
            });
            builder.Services.AddScoped<AudioService>(provider =>
              new AudioService(provider.GetService<IJSRuntime>(), provider.GetRequiredService<NetConnectConfig>()));
            builder.Services.AddScoped<ChatStateService>(provider =>
                new ChatStateService(provider.GetService<IJSRuntime>()));

            builder.Services.AddScoped<WebSocketService>(provider =>
            {

                return new WebSocketService(
                    provider.GetRequiredService<ChatStateService>(),
                    provider.GetService<IJSRuntime>(),
                    provider.GetRequiredService<AudioService>(),
                    provider.GetRequiredService<ILLMService>(),
                    provider.GetRequiredService<NetConnectConfig>());
            });


            builder.Services.AddSingleton<IMonitorPingInfoView, MonitorPingInfoView>();
            builder.Services.AddSingleton<IApiService>(provider =>
                {
                    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    string appDataDirectory = FileSystem.AppDataDirectory;
                    string nativeLibDir = string.Empty;
                    var browserHost = provider.GetRequiredService<IBrowserHost>();

#if ANDROID
                    nativeLibDir = Android.App.Application.Context.ApplicationInfo.NativeLibraryDir; 
#endif
                    var cmdProcessorProvider = provider.GetRequiredService<ICmdProcessorProvider>();
                    return new ApiService(loggerFactory, configuration, cmdProcessorProvider, appDataDirectory, nativeLibDir, browserHost);
                });

            builder.Services.AddSingleton<IAuthService>(provider =>
         {
             var logger = provider.GetRequiredService<ILogger<AuthService>>();
             var netConfig = provider.GetRequiredService<NetConnectConfig>();
             var rabbitRepo = provider.GetRequiredService<IRabbitRepo>();
             var processorStates = provider.GetRequiredService<LocalProcessorStates>();
             return new AuthService(logger, netConfig, rabbitRepo, processorStates);
         });
            builder.Services.AddSingleton<ICmdProcessorProvider>
                (provider =>
                {
                    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                    var rabbitRepo = provider.GetRequiredService<IRabbitRepo>();
                    var netConfig = provider.GetRequiredService<NetConnectConfig>();
                    var browserHost = provider.GetRequiredService<IBrowserHost>();
                    return new CmdProcessorProvider(loggerFactory, rabbitRepo, netConfig, browserHost);
                });
            builder.Services.AddSingleton<IPlatformService>(provider =>
            {
                var netConfig = provider.GetRequiredService<NetConnectConfig>();

#if ANDROID
				  var logger = provider.GetRequiredService<ILogger<AndroidPlatformService>>();
				   return new AndroidPlatformService(logger,netConfig);
#endif
#if WINDOWS
                var logger = provider.GetRequiredService<ILogger<WindowsPlatformService>>();
                 var backgroundService = provider.GetRequiredService<IBackgroundService>();
                return new WindowsPlatformService(backgroundService, logger,netConfig);
#endif
                // throw new NotImplementedException("Unsupported platform");
            });
#if WINDOWS
            builder.Services.AddSingleton<IBackgroundService>
                (provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<BackgroundService>>();
                    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                    var netConfig = provider.GetRequiredService<NetConnectConfig>();
                    var rabbitRepo = provider.GetRequiredService<IRabbitRepo>();
                    var fileRepo = provider.GetRequiredService<IFileRepo>();
                    var processorStates = provider.GetRequiredService<LocalProcessorStates>();
                    var cmdProcessorProvider = provider.GetRequiredService<ICmdProcessorProvider>();
                    var monitorPingInfoView = provider.GetRequiredService<IMonitorPingInfoView>();
                    var browserHost = provider.GetRequiredService<IBrowserHost>();
                    return new BackgroundService(logger, netConfig, loggerFactory, rabbitRepo, fileRepo, processorStates, monitorPingInfoView, cmdProcessorProvider,browserHost);
                });
#endif
        }
        private static void BuildViewModels(MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<ProcessorStatesViewModel>();
            builder.Services.AddSingleton<ScanProcessorStatesViewModel>();
            builder.Services.AddSingleton<MainPageViewModel>();
            builder.Services.AddSingleton<ConfigPageViewModel>();
            builder.Services.AddSingleton<ExitPageViewModel>();
        }
        private static void BuildPages(MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<ScanPage>();
            builder.Services.AddSingleton<NetworkMonitorPage>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<ConfigPage>();
            builder.Services.AddSingleton<ExitPage>();
            builder.Services.AddSingleton<DataViewPage>();
            builder.Services.AddSingleton<ChatPage>();
        }
        private static void ShowAlertBlocking(string title, string? message)
        {
            var fullMessage = string.IsNullOrWhiteSpace(message) ? title : $"{title}\n{message}";
            var dispatcher = ServiceInitializer.Dispatcher;
            dispatcher.Dispatch(() =>
            {
                var mainPage = Application.Current?.MainPage;
                if (mainPage != null)
                {
                    mainPage.DisplayAlert("Error", fullMessage, "OK").GetAwaiter().GetResult();
                }
                else
                {
                    // Fallback if MainPage is not available
                    Console.WriteLine(fullMessage);
                }
            });
        }


    }
}
