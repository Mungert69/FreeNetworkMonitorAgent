using NetworkMonitor.Objects;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.DTOs;
using Microsoft.Extensions.Logging;
namespace NetworkMonitorAgent;
public partial class DetailsPage : ContentPage
{

    private ILogger _logger;
    public DetailsPage(ILogger logger,IMonitorPingInfoView monitorPingInfoView)
    {
        try
        {
            InitializeComponent();
            _logger=logger;
            BindingContext = monitorPingInfoView;
        }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : Unable to load DetailsPage. Error was: {ex.Message}");
        }
    }

    private async void OnBackButton_Clicked(object sender, EventArgs e)
    {
        try {
        // Navigate back to the previous page
        await Shell.Current.Navigation.PopAsync();}
         catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : in OnBackButton_Clicked on DetailsPage. Error was: {ex.Message}");
        }
    }

}
