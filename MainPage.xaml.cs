using Microsoft.Extensions.Logging;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Maui;
using NetworkMonitor.Maui.Views;
using NetworkMonitor.Maui.ViewModels;
using NetworkMonitor.Maui.Controls;

namespace NetworkMonitorAgent;

public partial class MainPage : ContentPage
{
    private CancellationTokenSource _cancellationTokenSource;
    private readonly MainPageViewModel _mainPageViewModel;
    private readonly ILogger _logger; // Reintroduced logger

    public MainPage(ILogger logger,MainPageViewModel mainPageViewModel, ProcessorStatesViewModel processorStatesViewModel)
    {
        InitializeComponent();

        _logger = logger; // Store the logger

        _mainPageViewModel = mainPageViewModel;
        _cancellationTokenSource = new CancellationTokenSource();
        _mainPageViewModel.PollingCts = _cancellationTokenSource;
        _mainPageViewModel.ShowLoadingMessage += (sender, args) => ShowLoadingNoCancel(args);
       
       // _mainPageViewModel.SetupTasks();
        // Subscribe to VM events
        _mainPageViewModel.ShowAlertRequested += (sender, args) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await DisplayAlert(args.Title, args.Message, "OK"));
        };

        _mainPageViewModel.OpenBrowserRequested += (sender, url) =>
        {
            MainThread.BeginInvokeOnMainThread(async() =>
                await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred));
        };

        _mainPageViewModel.NavigateRequested += (sender, route) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await Shell.Current.GoToAsync(route));
        };


        TaskListView.ItemsSource = _mainPageViewModel.Tasks;

        BindingContext = _mainPageViewModel;
        CustomPopupView.BindingContext = processorStatesViewModel;
        ProcessorStatesView.BindingContext = processorStatesViewModel;
    }

    private void OnSwitchToggled(object sender, ToggledEventArgs e)
    {
        try
        {
            if (_mainPageViewModel != null)
            {
                _ = _mainPageViewModel.SetServiceStartedAsync(e.Value);
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Error in OnSwitchToggled: {ex.Message}", "OK");
            _logger.LogError(ex, "Error in OnSwitchToggled");
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
