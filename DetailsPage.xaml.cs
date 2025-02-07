using NetworkMonitor.Objects;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.DTOs;
using Microsoft.Extensions.Logging;
namespace NetworkMonitorAgent;
public partial class DetailsPage : ContentPage
{


    public DetailsPage(IMonitorPingInfoView monitorPingInfoView)
    {
        try
        {
            InitializeComponent();
            BindingContext = monitorPingInfoView;

        }
        catch (Exception ex)
        {
            }
    }

    private async void OnBackButton_Clicked(object sender, EventArgs e)
    {
        try {
        // Navigate back to the previous page
        await Shell.Current.Navigation.PopAsync();}
         catch (Exception ex)
        {
             }
    }

}
