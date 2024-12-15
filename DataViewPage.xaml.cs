
using NetworkMonitor.Processor.Services;
using NetworkMonitor.DTOs;
using NetworkMonitor.Objects;
using NetworkMonitor.Maui.Controls;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Connection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Collections.Specialized;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using CommunityToolkit.Maui.Views;

namespace NetworkMonitorAgent;
public partial class DateViewPage : ContentPage
{

    private ILogger _logger;
    private IMonitorPingInfoView _monitorPingInfoView;
    public DateViewPage(IMonitorPingInfoView monitorPingInfoView)
    {
        try
        {
            InitializeComponent();
            _logger = MauiProgram.ServiceProvider.GetRequiredService<ILogger<DateViewPage>>();
            _monitorPingInfoView = monitorPingInfoView;
            BindingContext = monitorPingInfoView;
        }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : Unable to load DataViewPage. Error was: {ex.Message}");
        }

    }

    private async void OnStatusIndicatorTapped(object sender, EventArgs e)
    {
        try
        {
            if (sender is View view && view.BindingContext is MPIndicator mpIndicator)
            {
                var monitorPingInfoView = BindingContext as IMonitorPingInfoView;
                monitorPingInfoView?.SelectMonitorPingInfo(mpIndicator.MonitorIPID);

                var monitorPingInfo = monitorPingInfoView?.SelectedMonitorPingInfo;
                if (monitorPingInfo != null)
                {
                    await ShowDetailsPopup(monitorPingInfo);
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : in OnStatusIndicatorTapped on DataViewPage. Error was: {ex.Message}");
        }
    }

    private async Task ShowDetailsPopup(MonitorPingInfo info)
    {
        try
        {
            var popup = new StatusDetailsPopup
            {
                BindingContext = info
            };

            var result = await this.ShowPopupAsync(popup, CancellationToken.None);

            if (result is bool boolResult)
            {
                if (boolResult)
                {

                    var detailsPage = new DetailsPage(_logger,_monitorPingInfoView);
                    await Shell.Current.Navigation.PushAsync(detailsPage);

                }

            }
        }
        catch (Exception e)
        {
            _logger.LogError($" Error: Could not navigate to page {nameof(DetailsPage)}. Error was: {e.ToString()}");
        }// Yes was tapped
    }
}




