using Microsoft.Maui.Controls;
using NetworkMonitor.Maui.Services;
using NetworkMonitor.Maui.ViewModels;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Objects;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace NetworkMonitorAgent;

public partial class ScanPage : ContentPage
{
    private readonly ILogger<ScanPage> _logger;
    private ScanProcessorStatesViewModel? _scanProcessorStatesViewModel;
    private readonly IUiDispatcher _dispatcher;
    public string FrontendUrl => AppConstants.FrontendUrl;
    private readonly IPlatformService _platformService;
    private readonly IServiceProvider _services;

    // Guards to avoid double initialization / double subscription
    private bool _viewModelInitialized = false;
    private bool _endpointHandlerAdded = false;

    public ScanPage(ILogger<ScanPage> logger, IServiceProvider services, IPlatformService platformService)
    {
        try
        {
            InitializeComponent();
            _logger = logger;
            _services = services;
            _platformService = platformService;
            _dispatcher = ServiceInitializer.Dispatcher;

            // Do not resolve ViewModel here. Resolve it lazily when authorised.
            UpdateVisibility();
        }
        catch (Exception ex)
        {
            _logger?.LogError($" Error : Unable to load ScanPage. Error was: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateVisibility();
    }

    public void UpdateVisibility()
    {
        try
        {
            // Use a synchronous dispatch to avoid async-lambda warnings.
            _dispatcher.Dispatch(() =>
            {
                bool isAuth = _platformService?.IsAuthorised ?? false;

                // Show/hide UI sections
                ScanView.IsVisible = isAuth;
                AgentDisabledMessage.IsVisible = !isAuth;

                // If authorised and ViewModel not set, resolve it now (after singletons should be ready)
                if (isAuth && _scanProcessorStatesViewModel == null)
                {
                    try
                    {
                        // Resolve the singleton VM and initialize it once.
                        _scanProcessorStatesViewModel = _services.GetService<ScanProcessorStatesViewModel>()
                                                       ?? _services.GetRequiredService<ScanProcessorStatesViewModel>();

                        if (!_viewModelInitialized)
                        {
                            try
                            {
                                _scanProcessorStatesViewModel.Initialize();
                            }
                            catch (Exception initEx)
                            {
                                _logger?.LogError(initEx, "Error initializing ScanProcessorStatesViewModel from ScanPage");
                            }
                            _viewModelInitialized = true;
                        }

                        BindingContext = _scanProcessorStatesViewModel;
                        CustomPopupView.BindingContext = _scanProcessorStatesViewModel;

                        if (!_endpointHandlerAdded)
                        {
                            EndpointTypePicker.SelectedIndexChanged += OnEndpointTypePickerSelectedIndexChanged;
                            _endpointHandlerAdded = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $" Error resolving ScanProcessorStatesViewModel: {ex.Message}");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $" Error : in UpdateVisibility on ScanPage. Error was: {ex.Message}");
        }
    }

    private void OnEndpointTypePickerSelectedIndexChanged(object? sender, EventArgs e)
    {
        try
        {
            if (_scanProcessorStatesViewModel == null)
            {
                _logger?.LogWarning("Endpoint type changed but ViewModel is not initialized.");
                return;
            }

            if (EndpointTypePicker.SelectedItem is string selectedEndpointType)
            {
                _scanProcessorStatesViewModel.DefaultEndpointType = selectedEndpointType;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $" Error : in OnEndpointTypePickerSelectedIndexChanged on ScanPage. Error was: {ex.Message}");
        }
    }

    private async void OpenLoginWebsite()
    {
        try
        {
            await Browser.Default.OpenAsync($"https://{AppConstants.AppDomain}/dashboard", BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            // Handle any exceptions
            await DisplayAlert("Error", $"Could not open browser . Error was : {ex.Message}", "OK");
            _logger?.LogError(ex, $"Could not open browser. Error was : {ex}");
        }
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        try
        {
            if (_scanProcessorStatesViewModel == null)
            {
                await DisplayAlert("Agent not ready", "Agent not authorised or ViewModel not initialized yet.", "OK");
                return;
            }

            ScanSection.IsVisible = false;
            LoadingSection.IsVisible = true;
            ResultsSection.IsVisible = false;

            var detectedHosts = await _scanProcessorStatesViewModel.ScanForHosts();

            LoadingSection.IsVisible = false;
            if (_scanProcessorStatesViewModel != null)
            {
                _scanProcessorStatesViewModel.IsPopupVisible = false;
            }

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
            _logger?.LogError(ex, $"Could not scan local hosts. Error was: {ex}");
        }
    }

    private async void OnAddServicesClicked(object sender, EventArgs e)
    {
        try
        {
            if (_scanProcessorStatesViewModel == null)
            {
                await DisplayAlert("Agent not ready", "Agent not authorised or ViewModel not initialized yet.", "OK");
                return;
            }

            await _scanProcessorStatesViewModel.AddServices();
            await DisplayAlert("Success", $"Added {_scanProcessorStatesViewModel.SelectedDevices.Count} services to be monitored. You will receive alerts if any of these servers are down. View host monitoring details under the Monitored Hosts tab. Alternatively you can manage and view more detailed host data at https://{AppConstants.AppDomain}/dashboard. Login using the same email you registerd this agent with.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not add services. Error was: {ex.Message}", "OK");
            _logger?.LogError(ex, $"Could not add services. Error was: {ex}");
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
            _logger?.LogError(ex, $"Could not clear services. Error was: {ex}");
        }
    }

    private async void OnCheckServicesClicked(object sender, EventArgs e)
    {
        try
        {
            if (_scanProcessorStatesViewModel == null)
            {
                await DisplayAlert("Agent not ready", "Agent not authorised or ViewModel not initialized yet.", "OK");
                return;
            }

            LoadingSection.IsVisible = true;
            ResultsSection.IsVisible = false;
            await _scanProcessorStatesViewModel.CheckServices();
            LoadingSection.IsVisible = false;
            ResultsSection.IsVisible = true;
            _scanProcessorStatesViewModel.IsPopupVisible = false;

            var checkedCount = _scanProcessorStatesViewModel.SelectedDevices.Count;
            var message = checkedCount > 0
                ? $"Checked {checkedCount} services. Review the latest status in the result output below."
                : "Service checks completed. Review the result output below.";

            await DisplayAlert("Checks complete", message, "Show results");
            await BringResultsIntoViewAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not check services. Error was: {ex.Message}", "OK");
            _logger?.LogError(ex, $"Could not check services. Error was: {ex.Message}");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        try
        {
            if (_scanProcessorStatesViewModel == null)
            {
                await DisplayAlert("Agent not ready", "Agent not authorised or ViewModel not initialized yet.", "OK");
                return;
            }

            await _scanProcessorStatesViewModel.Cancel();
            LoadingSection.IsVisible = false;
            ScanSection.IsVisible = true;
            if (_scanProcessorStatesViewModel != null)
            {
                _scanProcessorStatesViewModel.IsPopupVisible = false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not complete Cancel click. Error was: {ex.Message}", "OK");
            _logger?.LogError(ex, $"Could not complete Cancel click. Error was: {ex}");
        }
    }

    private async Task BringResultsIntoViewAsync()
    {
        try
        {
            await Task.Delay(100);
            OutputScrollView?.Focus();

            if (CompletedMessageLabel != null && OutputScrollView != null)
            {
                await OutputScrollView.ScrollToAsync(CompletedMessageLabel, ScrollToPosition.End, true);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to bring scan results into view.");
        }
    }

    private void OnHostsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_scanProcessorStatesViewModel == null) return;

            var selectedHosts = e.CurrentSelection.Cast<MonitorIP>().ToList();
            if (selectedHosts != null && selectedHosts.Count > 0)
            {
                _scanProcessorStatesViewModel.AddSelectedHosts(selectedHosts);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $" Error : in OnHostsSelectionChanged on ScanPage. Error was: {ex.Message}");
        }
    }

    private async void OnGoHomeClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//Home");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $" Error : in OnGoHomeClicked on LogsPage. Error was: {ex.Message}");
        }
    }
}
