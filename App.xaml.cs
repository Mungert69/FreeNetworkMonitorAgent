using MetroLog.Maui;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Maui.Utils;
namespace NetworkMonitorAgent;

public partial class App : Application
{
    public App(IServiceProvider serviceProvider)
    {   
        try
        {        
            InitializeComponent();
            MainPage = serviceProvider.GetRequiredService<AppShell>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing App: {ex.Message}");
        }
    }

    protected override void OnStart()
    {
        base.OnStart();
        LogController.InitializeNavigation(
            page => MainPage!.Navigation.PushModalAsync(page),
            () => MainPage!.Navigation.PopModalAsync());
    }
}
