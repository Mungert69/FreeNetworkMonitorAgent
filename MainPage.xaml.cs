using Microsoft.Extensions.Logging;
using NetworkMonitorAgent.ViewModels;

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
        _mainPageViewModel.ShowLoadingMessage += (sender, show) => ShowLoadingNoCancel(show);
        _mainPageViewModel.AuthorizeAction += Authorize;
        _mainPageViewModel.LoginAction += OpenLoginWebsite;
        _mainPageViewModel.AddHostsAction += ScanHosts;
        _mainPageViewModel.SetupTasks();

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

    private async void Authorize()
    {
        try
        {
            var result = await _mainPageViewModel.AuthorizeAsync();
            if (!result.Success)
            {
                await DisplayAlert("Error", result.Message, "OK");
                _mainPageViewModel.IsPolling = false;
                return;
            }

            if (!string.IsNullOrWhiteSpace(_mainPageViewModel.AuthUrl))
            {
                PollForTokenInBackground();
                await Browser.Default.OpenAsync(_mainPageViewModel.AuthUrl, BrowserLaunchMode.SystemPreferred);
            }
            else
            {
                await DisplayAlert("Error", "Authorization URL is not available.", "OK");
                _logger.LogError("Authorization URL is not available");
                _mainPageViewModel.IsPolling = false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not authorize. Error: {ex.Message}", "OK");
            _logger.LogError(ex, "Could not authorize");
            _mainPageViewModel.IsPolling = false;
        }
    }

    private async void OpenLoginWebsite()
    {
        try
        {
            var result = await _mainPageViewModel.OpenLoginWebsiteAsync();
            if (result.Success && !string.IsNullOrWhiteSpace(result.Message))
            {
                await Browser.Default.OpenAsync(result.Message, BrowserLaunchMode.SystemPreferred);
            }
            else
            {
                await DisplayAlert("Error", "Login URL is not available.", "OK");
                _logger.LogError("Login URL is not available");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not open login website. Error: {ex.Message}", "OK");
            _logger.LogError(ex, "Could not open login website");
        }
    }

    private async void ScanHosts()
    {
        try
        {
            var result = await _mainPageViewModel.ScanHostsAsync();
            if (result.Success && !string.IsNullOrWhiteSpace(result.Message))
            {
                await Shell.Current.GoToAsync(result.Message);
            }
            else
            {
                await DisplayAlert("Error", "Navigation URL is not available.", "OK");
                _logger.LogError("Navigation URL is not available");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not navigate to scan page. Error: {ex.Message}", "OK");
            _logger.LogError(ex, "Could not navigate to scan page");
        }
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            _mainPageViewModel.IsPolling = false;
            _cancellationTokenSource?.Cancel();
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

    private void ShowLoadingNoCancel(bool show)
    {
        try
        {
            ProgressIndicator.IsVisible = show;
            ProgressIndicator.IsRunning = show;
            CancelButton.IsVisible = false;
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Could not update loading indicators. Error: {ex.Message}", "OK");
            _logger.LogError(ex, "Could not update loading indicators in ShowLoadingNoCancel");
        }
    }

    private async void PollForTokenInBackground()
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            ShowLoading(true);
            var result = await _mainPageViewModel.PollForTokenAsync(_cancellationTokenSource.Token);
            ShowLoading(false);
            _mainPageViewModel.IsPolling = false;

            if (result.Success)
            {
                await DisplayAlert("Success", $"Authorization successful! Now login and add hosts using '{_mainPageViewModel.MonitorLocation}' as the monitor location.", "OK");
            }
            else
            {
                await DisplayAlert("Fail", result.Message, "OK");
                _logger.LogError($"PollForToken failed: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error while polling for token: {ex.Message}", "OK");
            _logger.LogError(ex, "Error while polling for token");
            _mainPageViewModel.IsPolling = false;
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}
