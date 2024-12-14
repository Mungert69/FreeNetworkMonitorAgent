using MetroLog.Maui;
using NetworkMonitor.Processor.Services;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using Microsoft.Maui.Graphics;  // For Rectangle
using Microsoft.Maui.Layouts;  // For AbsoluteLayoutFlags

namespace NetworkMonitorAgent;
public partial class ConfigPage : ContentPage
{
    public ConfigPage(ConfigPageViewModel configPageViewModel)
    {
        try
        {
            InitializeComponent();
            BindingContext = configPageViewModel;
        }
        catch (Exception ex)
        {
           
        }
    }
}

