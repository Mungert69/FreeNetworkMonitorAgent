using MetroLog.Maui;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui.ViewModels;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Maui;
using NetworkMonitor.Maui.Views;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Maui.Controls;
namespace NetworkMonitorAgent;

public partial class App : Application
{


   // public static ProcessorStatesViewModel ProcessorStatesVM { get; private set; }
    private ILogger? _logger;

    public App(IServiceProvider serviceProvider)
    {   
        try
        {        
            InitializeComponent();
            _logger = serviceProvider.GetRequiredService<ILogger<App>>();
            MainPage = serviceProvider.GetRequiredService<AppShell>(); ;

            //ProcessorStatesVM = new ProcessorStatesViewModel();
            LogController.InitializeNavigation(
           page => MainPage!.Navigation.PushModalAsync(page),
           () => MainPage!.Navigation.PopModalAsync());
        }
        catch (Exception ex)
        {
            _logger?.LogError($" Error initializing App {ex.Message} ");
        }



    }
}
