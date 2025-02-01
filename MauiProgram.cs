using MetroLog.MicrosoftExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Connection;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Api.Services;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui;
using NetworkMonitor.Objects;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Utils.Helpers;
using CommunityToolkit.Maui;



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
                    loggingBuilder.AddInMemoryLogger(options =>
                    {
                        options.MaxLines = 1024;
                        options.MinLevel = LogLevel.Information;
                        options.MaxLevel = LogLevel.Critical;
                    });
                });


            }
            catch (Exception ex)
            {
                ExceptionHelper.HandleGlobalException(ex, " Error : could not setup logging");
            }


            try
            {
                LoadConfiguration(builder, os);
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
        private static void LoadConfiguration(MauiAppBuilder builder, string os)
        {
            IConfigurationRoot? config = null;
            try
            {
                string localAppSettingsPath = Path.Combine(FileSystem.AppDataDirectory, $"appsettings.json");
                //string packagedAppSettingsPath = "NetworkMonitorAgent.appsettings.json";

                // Check if a local copy of appsettings.json exists
                if (File.Exists(localAppSettingsPath))
                {
                    // Use the local copy
                    config = new ConfigurationBuilder()
                        .AddJsonFile(localAppSettingsPath, optional: false, reloadOnChange: false)
                        .Build();
                }
                else
                {
                    using var stream = FileSystem.OpenAppPackageFileAsync($"appsettings.json").Result;
                    config = new ConfigurationBuilder().AddJsonStream(stream).Build();
                }
                builder.Configuration.AddConfiguration(config);
            }
            catch (Exception ex)
            {
                ExceptionHelper.HandleGlobalException(ex, $" Error could not load appsetting.json");
            }
            try
            {
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
            builder.Services.AddSingleton<LocalProcessorStates>(provider =>
         {
             return new LocalProcessorStates();
         });

            builder.Services.AddSingleton<IFileRepo>(provider =>
            {

                try
                {

                    bool isRunningOnMauiAndroid = true;
                    string prefixPath = FileSystem.AppDataDirectory;
                    var fileRepo = new FileRepo(isRunningOnMauiAndroid, prefixPath);
                    return fileRepo;

                }
                catch (Exception ex)
                {
                    ExceptionHelper.HandleGlobalException(ex, "Error : initializing FileRepo");
                    return new FileRepo();

                }

            });
            builder.Services.AddSingleton<IRabbitRepo>(provider =>
           {
               var logger = provider.GetRequiredService<ILogger<RabbitRepo>>();
               var netConfig = provider.GetRequiredService<NetConnectConfig>();
               // Choose the appropriate constructor
               return new RabbitRepo(logger, netConfig);
           });


            builder.Services.AddSingleton<NetConnectConfig>(provider =>
            {
                // Assuming Configuration is properly set up
                var configuration = provider.GetRequiredService<IConfiguration>();
                var appDataDirectory = FileSystem.AppDataDirectory;
                return new NetConnectConfig(configuration, appDataDirectory);
            });


        }

        private static void BuildServices(MauiAppBuilder builder)
        {

            builder.Services.AddSingleton<IMonitorPingInfoView, MonitorPingInfoView>();

            builder.Services.AddSingleton<IApiService>(provider =>
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var cmdProcessorProvider = provider.GetRequiredService<ICmdProcessorProvider>();

        return new ApiService(loggerFactory, configuration, cmdProcessorProvider, FileSystem.AppDataDirectory);
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

                    return new CmdProcessorProvider(loggerFactory, rabbitRepo, netConfig);


                });


            builder.Services.AddSingleton<IPlatformService>(provider =>
            {
#if ANDROID
				  var logger = provider.GetRequiredService<ILogger<AndroidPlatformService>>();
				   return new AndroidPlatformService(logger);
#endif

#if WINDOWS
                var logger = provider.GetRequiredService<ILogger<WindowsPlatformService>>();
                 var backgroundService = provider.GetRequiredService<IBackgroundService>();
                return new WindowsPlatformService(backgroundService, logger);
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

                    return new BackgroundService(logger, netConfig, loggerFactory, rabbitRepo, fileRepo, processorStates, monitorPingInfoView, cmdProcessorProvider);


                });
#endif


        }

        private static void BuildViewModels(MauiAppBuilder builder)
        {

            builder.Services.AddSingleton<ProcessorStatesViewModel>();
            builder.Services.AddSingleton<ScanProcessorStatesViewModel>();
            builder.Services.AddSingleton<MainPageViewModel>();
            builder.Services.AddSingleton<ConfigPageViewModel>();

        }
        private static void BuildPages(MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<ScanPage>();
            builder.Services.AddSingleton<NetworkMonitorPage>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<ConfigPage>();
            builder.Services.AddSingleton<DataViewPage>();


        }
        private static void ShowAlertBlocking(string title, string? message)
        {
            var fullMessage = string.IsNullOrWhiteSpace(message) ? title : $"{title}\n{message}";

            MainThread.BeginInvokeOnMainThread(() =>
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