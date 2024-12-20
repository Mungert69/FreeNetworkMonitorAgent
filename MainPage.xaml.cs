using Microsoft.Extensions.Logging;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Maui;
using NetworkMonitorAgent.Views;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Maui.Controls;

namespace NetworkMonitorAgent;

public partial class MainPage : ContentPage
{
    private CancellationTokenSource _cancellationTokenSource;
    private readonly MainPageViewModel _mainPageViewModel;
    private readonly ILogger _logger; // Reintroduced logger
private bool _isUpdatingSwitch = false;
    public MainPage(ILogger logger, MainPageViewModel mainPageViewModel, ProcessorStatesViewModel processorStatesViewModel)
    {
        InitializeComponent();

        _logger = logger;

        _mainPageViewModel = mainPageViewModel;
        BindingContext = _mainPageViewModel;
        CustomPopupView.BindingContext = processorStatesViewModel;
        ProcessorStatesView.BindingContext = processorStatesViewModel;


        _cancellationTokenSource = new CancellationTokenSource();
        _mainPageViewModel.PollingCts = _cancellationTokenSource;
        _mainPageViewModel.ShowLoadingMessage += (sender, args) => ShowLoadingNoCancel(args);

        _mainPageViewModel.ShowAlertRequested += (sender, args) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await DisplayAlert(args.Title, args.Message, "OK"));
        };

        _mainPageViewModel.OpenBrowserRequested += (sender, url) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred));
        };

        _mainPageViewModel.NavigateRequested += (sender, route) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await Shell.Current.GoToAsync(route));
        };


        TaskListView.ItemsSource = _mainPageViewModel.Tasks;


    }

    private async void OnSwitchToggled(object sender, ToggledEventArgs e)
    {
       
        if (_isUpdatingSwitch)
        {
            return; // Ignore programmatic changes
        }
       
            var switchControl = (Switch)sender;
            bool originalState = switchControl.IsToggled;
       
        try
        {
            _isUpdatingSwitch = true;
            // Disable the switch temporarily while the service is starting

            switchControl.IsEnabled = false;
            _mainPageViewModel.ServiceMessage = "Starting service...";
            ShowLoading(true);

            // Attempt to start/stop the service
            bool isStarted = await _mainPageViewModel.SetServiceStartedAsync(e.Value);

            // Reflect the actual service state on the toggle
            switchControl.IsToggled = isStarted;

            // Update UI feedback
            _mainPageViewModel.ServiceMessage = isStarted ? "Service is running" : "Failed to start service.";
        }
        catch (Exception ex)
        {
            // Log error and notify the user
            _logger.LogError(ex, "Error in OnSwitchToggled");
            await DisplayAlert("Error", $"Failed to change service state: {ex.Message}", "OK");

            // Revert the toggle to its previous state
            switchControl.IsToggled = originalState;
        }
        finally
        {
            _isUpdatingSwitch = false;
            switchControl.IsEnabled = true;
            ShowLoading(false);
        }
    }


    private void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            _mainPageViewModel.IsPolling = false;
            // Cancel polling and reset CancellationTokenSource
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            _mainPageViewModel.PollingCts = _cancellationTokenSource;
            CancelButton.IsVisible = false;
            ShowLoading(false);
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Could not complete Cancel. Error: {ex.Message}", "OK");
            _logger.LogError(ex, "Could not complete Cancel");
        }
    }

    private void ShowLoading(bool show)
    {
        try
        {
            ProgressIndicator.IsVisible = show;
            ProgressIndicator.IsRunning = show;
            CancelButton.IsVisible = show;
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Could not update loading indicators. Error: {ex.Message}", "OK");
            _logger.LogError(ex, "Could not update loading indicators in ShowLoading");
        }
    }

    private void ShowLoadingNoCancel((bool show, bool showCancel) args)
    {
        try
        {
            (bool show, bool showCancel) = args;
            ProgressIndicator.IsVisible = show;
            ProgressIndicator.IsRunning = show;
            CancelButton.IsVisible = showCancel;
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Could not update loading indicators. Error: {ex.Message}", "OK");
            _logger.LogError(ex, "Could not update loading indicators in ShowLoadingNoCancel");
        }
    }

}
