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
    private readonly ILogger _logger;
    private bool _isUpdatingSwitch = false;

    public MainPage(ILogger logger, MainPageViewModel mainPageViewModel, ProcessorStatesViewModel processorStatesViewModel)
    {
        InitializeComponent();

        _logger = logger;

        _mainPageViewModel = mainPageViewModel;
        BindingContext = _mainPageViewModel;
        // CustomPopupView.BindingContext = processorStatesViewModel;
        // ProcessorStatesView.BindingContext = processorStatesViewModel;

        TaskListView.ItemsSource = _mainPageViewModel.Tasks;
        _cancellationTokenSource = new CancellationTokenSource();
        _mainPageViewModel.PollingCts = _cancellationTokenSource;

        _mainPageViewModel.ShowLoadingMessage += (sender, args) =>
        {
            (bool show, bool showCancel) = args;
            MainThread.BeginInvokeOnMainThread( () =>
            {
                ProgressIndicator.IsVisible = show;
                ProgressIndicator.IsRunning = show;
                CancelButton.IsVisible = showCancel;
            }
            );
        };


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





    }

    private async void OnSwitchToggled(object sender, ToggledEventArgs e)
    {
        try
        {
            // Safely handle casting the sender to Switch
            var switchControl = sender as Switch ?? throw new InvalidCastException("Sender is not a Switch.");

            bool originalState = switchControl.IsToggled;

            // Prevent re-entry for programmatic changes
            if (_isUpdatingSwitch)
            {
                return;
            }

            _isUpdatingSwitch = true;
            // Check if assets are ready
            if (!RootNamespaceProvider.AssetsReady)
            {
                switchControl.IsToggled = false;
                await DisplayAlert("Warning",
                    "Resource files still copying please wait. Check out the Setup Guide for information on the app's features. You may find the Network Monitor Assistant interesting.",
                    "OK");
                _isUpdatingSwitch = false;
                return;
            }

            try
            {
                // Disable the switch temporarily while the service is starting
                switchControl.IsEnabled = false;
               
                _mainPageViewModel.ServiceMessage = "Starting service...";
                ShowLoading(true);

                // Attempt to start/stop the service
                bool isStarted = await _mainPageViewModel.SetServiceStartedAsync(e.Value);

                // Reflect the actual service state on the toggle
                switchControl.IsToggled = isStarted;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Service operation was canceled.");
                await DisplayAlert("Operation Canceled", "The operation was canceled by the user.", "OK");
                switchControl.IsToggled = originalState;
            }
            catch (TimeoutException)
            {
                _logger.LogError("Service operation timed out.");
                await DisplayAlert("Timeout", "The operation timed out. Please try again later.", "OK");
                switchControl.IsToggled = originalState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in OnSwitchToggled");
                await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
                switchControl.IsToggled = originalState;
            }
            finally
            {
                // Ensure cleanup is always performed
                _isUpdatingSwitch = false;
                switchControl.IsEnabled = true;
                ShowLoading(false);
            }
        }
        catch (InvalidCastException ex)
        {
            _logger.LogError(ex, "Sender was not a Switch.");
            await DisplayAlert("Error", "An internal error occurred. Please restart the app.", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in initial OnSwitchToggled logic");
            await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
        }
    }


    private void ShowLoading(bool show) {
        ProgressIndicator.IsVisible = show;
        ProgressIndicator.IsRunning = show;
        CancelButton.IsVisible = show;
    }


    private async void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _mainPageViewModel.PollingCts = _cancellationTokenSource;
            CancelButton.IsVisible = false;
            ShowLoading(false);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not complete Cancel. Error: {ex.Message}", "OK");
            _logger.LogError(ex, "Could not complete Cancel");
        }
    }

}
