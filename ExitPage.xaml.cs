using Microsoft.Maui.Controls;
using NetworkMonitor.Maui.ViewModels;

namespace NetworkMonitorAgent;

public partial class ExitPage : ContentPage
{
    public ExitPage(ExitPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
