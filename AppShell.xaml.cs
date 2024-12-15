using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Objects.Repository;
using NetworkMonitor.Objects.ServiceMessage;
using NetworkMonitor.Utils.Helpers;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui;
using NetworkMonitor.Maui.Views;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Maui.Controls;
using MetroLog.Maui;
namespace NetworkMonitorAgent;
public partial class AppShell : Shell
{
    IPlatformService? _platformService;
    private ILogger? _logger;
    public AppShell(ILogger? logger, IPlatformService? platformService)
    {
        try
        {
          
            InitializeComponent();
            _logger = logger;
            _platformService = platformService;

             }
        catch (Exception ex)
        {
           _logger?.LogError($" Error initializing AppShell {ex.Message} ");
        }
    }
    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();
           if (_platformService!=null)  _platformService.RequestPermissionsAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError($" Error requesting permissions {ex.Message} ");
        }


    }
}
