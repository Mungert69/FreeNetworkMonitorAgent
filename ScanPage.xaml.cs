
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui.ViewModels;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Objects;

namespace NetworkMonitorAgent;

public partial class ScanPage : ContentPage
{

    private ILogger _logger;

    private ScanProcessorStatesViewModel _scanProcessorStatesViewModel;

    private IPlatformService _platformService;

    public ScanPage(ILogger logger, ScanProcessorStatesViewModel scanProcessorStatesViewModel, IPlatformService platformService)
    {
        try
        {
            InitializeComponent();
            _scanProcessorStatesViewModel = scanProcessorStatesViewModel;
            _logger = logger;
            _platformService = platformService;
            CustomPopupView.BindingContext = scanProcessorStatesViewModel;
            BindingContext = scanProcessorStatesViewModel;
            EndpointTypePicker.SelectedIndexChanged += OnEndpointTypePickerSelectedIndexChanged;

            UpdateVisibility();
        }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : Unable to load ScanPage. Error was: {ex.Message}");
        }

    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Update _isAgentEnabled when the page appears
        UpdateVisibility();
    }

    public void UpdateVisibility()
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
                     {
                         ScanView.IsVisible = _platformService.IsServiceStarted;
                         AgentDisabledMessage.IsVisible = !_platformService.IsServiceStarted;
                     });
        }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : in UpdateVisibility on ScanPage. Error was: {ex.Message}");
        }

    }

    private void OnEndpointTypePickerSelectedIndexChanged(object? sender, EventArgs e)
    {
        try
        {
            if (EndpointTypePicker.SelectedItem is string selectedEndpointType)
            {
                _scanProcessorStatesViewModel.DefaultEndpointType = selectedEndpointType;
            }
        }
          catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : in OnEndpointTypePickerSelectedIndexChanged on ScanPage. Error was: {ex.Message}");
        }
    }

    private async void OpenLoginWebsite()
    {

        try
        {//await Launcher.OpenAsync("https://freenetworkmonitor.click/dashboard");
            await Browser.Default.OpenAsync("https://freenetworkmonitor.click/dashboard", BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            await DisplayAlert("Error", $"Could not open browser . Error was : {ex.Message}", "OK");
            _logger.LogError($"Could not open browser. Error was : {ex.ToString()}");

        }

    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        try
        {
            ScanSection.IsVisible = false;
            LoadingSection.IsVisible = true;
            ResultsSection.IsVisible = false;

            var detectedHosts = await _scanProcessorStatesViewModel.ScanForHosts();

            LoadingSection.IsVisible = false;

            if (detectedHosts != null && detectedHosts.Count > 0)
            {
                HostsCollectionView.ItemsSource = detectedHosts;
                ResultsSection.IsVisible = true;
            }
            else
            {
                await DisplayAlert("No Hosts Found", "No hosts were found during the scan.", "OK");
                ScanSection.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            LoadingSection.IsVisible = false;
            ScanSection.IsVisible = true;
            await DisplayAlert("Error", $"Could not scan local hosts. Error was: {ex.Message}", "OK");
            _logger.LogError($"Could not scan local hosts. Error was: {ex}");
        }
    }

    private async void OnAddServicesClicked(object sender, EventArgs e)
    {
        try
        {
            await _scanProcessorStatesViewModel.AddServices();
            await DisplayAlert("Success", $"Added {_scanProcessorStatesViewModel.SelectedDevices.Count} services to be monitored. You will receive alerts if any of these servers are down. View host monitoring details under the Monitored Hosts tab. Alternatively you can manage and view more detailed host data at https://freenetworkmonitor.click/dashboard. Login using the same email you registerd this agent with.", "OK");

        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not add services. Error was: {ex.Message}", "OK");
            _logger.LogError($"Could not add services. Error was: {ex}");
        }
    }

    private async void OnClearServicesClicked(object sender, EventArgs e)
    {
        try
        {
            ResultsSection.IsVisible = false;
            ScanSection.IsVisible = true;
            HostsCollectionView.SelectedItems.Clear();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not clear services. Error was: {ex.Message}", "OK");
            _logger.LogError($"Could not clear services. Error was: {ex}");
        }
    }


    private async void OnCheckServicesClicked(object sender, EventArgs e)
    {
        try
        {
            LoadingSection.IsVisible = true;
            ResultsSection.IsVisible = false;
            await _scanProcessorStatesViewModel.CheckServices();
            await DisplayAlert("Success", $"Checked {_scanProcessorStatesViewModel.SelectedDevices.Count} Services. Check the Result Output for the status of each service checked.", "OK");
            LoadingSection.IsVisible = false;
            ResultsSection.IsVisible = true;

        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not check services. Error was: {ex.Message}", "OK");
            _logger.LogError($"Could not check services. Error was: {ex.Message}");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            await _scanProcessorStatesViewModel.Cancel();
            LoadingSection.IsVisible = false;
            ScanSection.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not complete Cancel click. Error was: {ex.Message}", "OK");
            _logger.LogError($"Could not complete Cancel click. Error was: {ex}");
        }
    }
    private void OnHostsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try {
        var selectedHosts = e.CurrentSelection.Cast<MonitorIP>().ToList();
        if (selectedHosts != null && selectedHosts.Count > 0)
        {
            _scanProcessorStatesViewModel.AddSelectedHosts(selectedHosts);
        }
        }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : in OnHostsSelectionChanged on ScanPage. Error was: {ex.Message}");
        }

        
    }
    private async void OnGoHomeClicked(object sender, EventArgs e)
    {
        try {
        await Shell.Current.GoToAsync("//Home");
    }
        catch (Exception ex)
        {
            if (_logger != null) _logger.LogError($" Error : in OnGoHomeClicked on LogsPage. Error was: {ex.Message}");
        }
    }
}

