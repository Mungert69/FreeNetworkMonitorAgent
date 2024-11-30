using NetworkMonitor.Objects;
using NetworkMonitorAgent.ViewModels;
using NetworkMonitor.DTOs;
namespace NetworkMonitorAgent;
public partial class DetailsPage : ContentPage
{
    public DetailsPage(IMonitorPingInfoView monitorPingInfoView)
    {
        InitializeComponent();
        BindingContext = monitorPingInfoView;
    }

 private async  void OnBackButton_Clicked(object sender, EventArgs e)
    {
        // Navigate back to the previous page
             await Shell.Current.Navigation.PopAsync();
    }
          
}
