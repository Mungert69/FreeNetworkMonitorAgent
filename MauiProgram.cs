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
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects;
using NetworkMonitor.Maui.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace NetworkMonitorAgent
{
    public static class MauiProgram
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static MauiApp CreateMauiApp()
        {
            ServiceInitializer.Initialize(new RootNamespaceProvider());

            var os = "linux";
#if ANDROID
			os="android";
#endif

#if WINDOWS
            os="windows";
#endif

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>().UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            // Add Maui Loggers
            try
            {
                builder.Logging.AddTraceLogger(options =>
                {
                    options.MinLevel = LogLevel.Information;
                    options.MaxLevel = LogLevel.Critical;
                });
                builder.Logging.AddInMemoryLogger(options =>
                {
                    options.MaxLines = 1024;
                    options.MinLevel = LogLevel.Information;
                    options.MaxLevel = LogLevel.Critical;
                });
                builder.Logging.AddStreamingFileLogger(options =>
                {
                    options.RetainDays = 2;
                    options.FolderPath = Path.Combine(
                        FileSystem.CacheDirectory,
                        "MetroLogs");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error : could not setup logging . Error was : {ex}");
            }
            // Define the paths for the local and packaged appsettings.json
            try
            {
                string localAppSettingsPath = Path.Combine(FileSystem.AppDataDirectory, $"appsettings.json");
                //string packagedAppSettingsPath = "NetworkMonitorAgent.appsettings.json";
                IConfigurationRoot config;
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
                Task.Run(async () =>
                {
                    string output = await CopyAssetsHelper.CopyAssetsToLocalStorage($"{config["OpensslVersion"]}-{os}", "cs-assets", "dlls");
                    Console.WriteLine(output);
                });


            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error could not load appsetting.json . Error was : {ex.Message}");
            }

            try
            {
                BuildRepoAndConfig(builder);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in BuildRepoAndConfig: {e}");
            }

            try
            {
                BuildServices(builder);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in BuildServices: {e}");
            }

            try
            {
                BuildViewModels(builder);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in BuildViewModels: {e}");
            }

            try
            {
                BuildPages(builder);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in BuildPages: {e}");
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
            catch (Exception e)
            {
                Console.WriteLine($"Error creating AppShell: {e}");
            }



            var app = builder.Build();
            ServiceProvider = app.Services;
            return app;
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
                    Console.WriteLine($"Error : initializing FileRepo . Error was : {ex}");
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
                    var cmdProcessorProvider=provider.GetRequiredService<ICmdProcessorProvider>();
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
            builder.Services.AddSingleton<ConfigPage>();
            builder.Services.AddSingleton<DateViewPage>();
        }

    }
}
