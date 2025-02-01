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
        // Centralized exception handler
       /* public static void HandleGlobalException(Exception exception, string title)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage?.DisplayAlert(title, exception.Message, "OK");
                Console.WriteLine($"Global Exception Caught: {exception}");
            });
        }*/

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

                builder.Logging.AddInMemoryLogger(options =>
                {
                    options.MaxLines = 1024;
                    options.MinLevel = LogLevel.Information;
                    options.MaxLevel = LogLevel.Critical;
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
                builder.Services.AddSingleton(provider =>
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
            builder.Services.AddSingleton(provider =>
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
				   //var dialogService = provider.GetRequiredService<IDialogService>();
				   return new AndroidPlatformService(logger);
#endif

#if WINDOWS
                var logger = provider.GetRequiredService<ILogger<WindowsPlatformService>>();
                //var dialogService = provider.GetRequiredService<IDialogService>();
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
            builder.Services.AddSingleton<IMonitorPingInfoView>(provider =>
           {
               return new MonitorPingInfoView();
           });
            builder.Services.AddSingleton(provider =>
                        {
                            var logger = provider.GetRequiredService<ILogger<ProcessorStatesViewModel>>();
                            var processorStates = provider.GetRequiredService<LocalProcessorStates>();
                            // Choose the appropriate constructor
                            return new ProcessorStatesViewModel(logger, processorStates);
                        });
            builder.Services.AddSingleton(provider =>
           {
               var logger = provider.GetRequiredService<ILogger<ScanProcessorStatesViewModel>>();
               var cmdProcessorProvider = provider.GetRequiredService<ICmdProcessorProvider>();
               var nmapCmdProcessorStates = cmdProcessorProvider.GetProcessorStates("Nmap");
               var netConfig = provider.GetRequiredService<NetConnectConfig>();
               var apiService = provider.GetRequiredService<IApiService>();
               return new ScanProcessorStatesViewModel(logger, nmapCmdProcessorStates, apiService, netConfig);
           });
            builder.Services.AddSingleton(provider =>
          {
              var netConfig = provider.GetRequiredService<NetConnectConfig>();
              var logger = provider.GetRequiredService<ILogger<MainPageViewModel>>();
              var platformService = provider.GetRequiredService<IPlatformService>();
              var authService = provider.GetRequiredService<IAuthService>();
              return new MainPageViewModel(netConfig, platformService, logger, authService);
          });

            builder.Services.AddSingleton(provider =>
            {
                var netConfig = provider.GetRequiredService<NetConnectConfig>();
                var logger = provider.GetRequiredService<ILogger<ConfigPageViewModel>>();
                return new ConfigPageViewModel(logger, netConfig);
            });

        }
        private static void BuildPages(MauiAppBuilder builder)
        {
            builder.Services.AddSingleton(provider =>
                        {
                            var scanProcessorStatesViewModel = provider.GetRequiredService<ScanProcessorStatesViewModel>();
                            var logger = provider.GetRequiredService<ILogger<ScanPage>>();
                            var platformService = provider.GetRequiredService<IPlatformService>();

                            return new ScanPage(logger, scanProcessorStatesViewModel, platformService);
                        });

            builder.Services.AddSingleton(provider =>
           {
               var apiService = provider.GetRequiredService<IApiService>();
               return new NetworkMonitorPage(apiService);
           });
            builder.Services.AddSingleton(provider =>
           {

               var logger = provider.GetRequiredService<ILogger<MainPage>>();
               var processorStatesViewModel = provider.GetRequiredService<ProcessorStatesViewModel>();
               var mainPageViewModel = provider.GetRequiredService<MainPageViewModel>();
               return new MainPage(logger, mainPageViewModel, processorStatesViewModel);
           });
            builder.Services.AddSingleton(provider =>
            {

                var logger = provider.GetRequiredService<ILogger<DataViewPage>>();
                var monitorPingInfoView = provider.GetRequiredService<IMonitorPingInfoView>();

                return new DataViewPage(logger, monitorPingInfoView);
            });
            builder.Services.AddSingleton(provider =>
            {
                var configPageViewModel = provider.GetRequiredService<ConfigPageViewModel>();
                return new ConfigPage(configPageViewModel);
            });
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
