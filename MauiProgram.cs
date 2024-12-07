using MetroLog.MicrosoftExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Connection;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Api.Services;
using NetworkMonitorAgent.Services;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Objects;
using NetworkMonitorAgent.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;


namespace NetworkMonitorAgent
{
    public static class MauiProgram
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static MauiApp CreateMauiApp()
        {

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
                Task.Run(() => CopyAssetsToLocalStorage($"{config["OpensslVersion"]}-{os}")).Wait();

                ListCopiedFiles();


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

                    return new CmdProcessorFactory(loggerFactory, rabbitRepo, netConfig);


                });

           
            builder.Services.AddSingleton<IPlatformService>(provider =>
            {
#if ANDROID
				 /*  var logger = provider.GetRequiredService<ILogger<AndroidPlatformService>>();
				   var dialogService = provider.GetRequiredService<IDialogService>();
				   return new AndroidPlatformService(dialogService, logger);*/
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
               return new MainPage( logger, mainPageViewModel, processorStatesViewModel);
           });
            builder.Services.AddSingleton<ConfigPage>();
            builder.Services.AddSingleton<DateViewPage>();
        }
        private static async Task CopyAssetsToLocalStorage(string directoryName)
        {
            try
            {
                Console.WriteLine($"Starting asset copy from : {directoryName}");

                string[] assetFiles = await ListAssetFiles(directoryName);

                string localPath = Path.Combine(FileSystem.AppDataDirectory, "openssl");
                Directory.CreateDirectory(localPath);
                string localFilePath = "";
                foreach (var assetFile in assetFiles)
                {
                    string assetFilePath = Path.Combine(directoryName, assetFile);
                    using (var stream = await FileSystem.OpenAppPackageFileAsync(assetFilePath))
                    {
                        if (stream == null)
                        {
                            Console.WriteLine($"Failed to open asset file: {assetFilePath}");
                            continue;
                        }

                        localFilePath = Path.Combine(localPath, assetFile);
                        string? localFileDirectory = Path.GetDirectoryName(localFilePath);

                        if (localFileDirectory != null && !Directory.Exists(localFileDirectory))
                            Directory.CreateDirectory(localFileDirectory);

                        using (var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
#if WINDOWS
                    if (IsWindowsBusyboxBinary(assetFile)) SetExecutablePermission(localFilePath);
                    if (IsWindowsOpenSSLBinary(assetFile)) SetExecutablePermission(localFilePath);
                    if (IsWindowsNmapBinary(assetFile)) SetExecutablePermission(localFilePath);
                     if (IsWindowsCurlBinary(assetFile)) SetExecutablePermission(localFilePath);
#else
                    if (IsBusyboxBinary(assetFile)) SetExecutablePermission(localFilePath);
                    if (IsOpenSSLBinary(assetFile)) SetExecutablePermission(localFilePath);
                    if (IsNmapBinary(assetFile)) SetExecutablePermission(localFilePath);
                    if (IsCurlBinary(assetFile)) SetExecutablePermission(localFilePath);
#endif


                    Console.WriteLine($"Copying file: {localFilePath}");
                }

                Console.WriteLine($"Directory copied to: {localPath}");
                SetLDLibraryPath(Path.Combine(localPath, "lib64"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying assets: {ex.Message}");
            }
        }
        private static bool IsOpenSSLBinary(string assetFile)
        {
            string trimmedPath = assetFile.Trim('/', '\\'); // Trim leading and trailing slashes
            return trimmedPath.EndsWith("/openssl", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsBusyboxBinary(string assetFile)
        {
            string trimmedPath = assetFile.Trim('/', '\\'); // Trim leading and trailing slashes
            return trimmedPath.EndsWith("/busybox", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsNmapBinary(string assetFile)
        {
            string trimmedPath = assetFile.Trim('/', '\\'); // Trim leading and trailing slashes
            return trimmedPath.EndsWith("/nmap", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsCurlBinary(string assetFile)
        {
            string trimmedPath = assetFile.Trim('/', '\\'); // Trim leading and trailing slashes
            return trimmedPath.EndsWith("/curl", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsWindowsOpenSSLBinary(string assetFile)
        {
            string trimmedPath = assetFile.Trim('/', '\\'); // Trim leading and trailing slashes
            return trimmedPath.EndsWith("/openssl.exe", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsWindowsBusyboxBinary(string assetFile)
        {
            string trimmedPath = assetFile.Trim('/', '\\'); // Trim leading and trailing slashes
            return trimmedPath.EndsWith("/busybox.exe", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsWindowsNmapBinary(string assetFile)
        {
            string trimmedPath = assetFile.Trim('/', '\\'); // Trim leading and trailing slashes
            return trimmedPath.EndsWith("/nmap.exe", StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsWindowsCurlBinary(string assetFile)
        {
            string trimmedPath = assetFile.Trim('/', '\\'); // Trim leading and trailing slashes
            return trimmedPath.EndsWith("/curl.exe", StringComparison.OrdinalIgnoreCase);
        }
        private static void SetExecutablePermission(string filePath)
        {
            try
            {
                Console.WriteLine($"Attempting to set executable permission for: {filePath}");

#if ANDROID
        NetworkMonitorAgent.Platforms.Android.PermissionsHelper.MakeFileExecutable(filePath);
#elif WINDOWS

                ProcessFileForSymbolicLink(filePath);
#else
                // Existing implementation for other platforms
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sh",
                        Arguments = $"-c \"chmod +x {filePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine($"chmod output: {output}");
                }
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"chmod error: {error}");
                }

                Console.WriteLine($"Set executable permission for: {filePath}");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set executable permission for {filePath}: {ex.Message}");
            }
        }

#if WINDOWS
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateSymbolicLink(
            string lpSymlinkFileName,
            string lpTargetFileName,
            int dwFlags);

        const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

        private static void CreateSymbolicLinkWindows(string symlinkPath, string targetPath, bool isDirectory)
        {
            bool result = CreateSymbolicLink(symlinkPath, targetPath, isDirectory ? SYMBOLIC_LINK_FLAG_DIRECTORY : 0);
            if (!result)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        private static void ProcessFileForSymbolicLink(string filePath)
        {
            // Check if the file path ends with .exe
            if (filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                // Remove the .exe extension
                string targetPath = Path.ChangeExtension(filePath, null);

                // Create a symbolic link
                CreateSymbolicLinkWindows(filePath, targetPath, false);
            }
            else
            {
                // Handle cases where the file does not end with .exe if needed
                Console.WriteLine($"File does not end with .exe: {filePath}");
            }
        }
#endif


        private static void SetLDLibraryPath(string libraryPath)
        {
            try
            {
                string ldLibraryPath = $"LD_LIBRARY_PATH={libraryPath}";
                Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", libraryPath);
                Console.WriteLine($"Set LD_LIBRARY_PATH to: {libraryPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set LD_LIBRARY_PATH: {ex.Message}");
            }
        }

        private static async Task<string[]> ListAssetFiles(string directoryName)
        {
            try
            {
                string manifestFileName = Path.Combine(directoryName, "assets_manifest.txt");
                using (var stream = await FileSystem.OpenAppPackageFileAsync(manifestFileName))
                using (var reader = new StreamReader(stream))
                {
                    var fileList = new List<string>();

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            // Trim any leading ./ from the assetFilePath
                            line = line.TrimStart('.', '/');
                            fileList.Add(line);
                        }
                    }

                    return fileList.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading asset manifest: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static void ListCopiedFiles()
        {
            try
            {
                string localPath = Path.Combine(FileSystem.AppDataDirectory, "openssl");
                string[] files = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    Console.WriteLine($"File in local storage: {file}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing copied files: {ex.Message}");
            }
        }

    }
}
