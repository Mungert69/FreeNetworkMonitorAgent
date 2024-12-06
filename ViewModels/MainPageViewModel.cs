using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetworkMonitor.Objects;
using NetworkMonitor.Connection;
using System.Windows.Input;
using NetworkMonitorAgent.Services;
using Microsoft.Extensions.Logging;
using NetworkMonitor.Processor.Services;
using OpenAI.ObjectModels.ResponseModels;

namespace NetworkMonitorAgent.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private NetConnectConfig _netConfig;
        private IPlatformService _platformService;
        private ILogger _logger;
        private IAuthService _authService;  // Added
        private CancellationTokenSource? _pollingCts;

        public Action? AuthorizeAction;
        public Action? LoginAction;
        public Action? AddHostsAction;
        public ICommand ToggleServiceCommand { get; }
        public event EventHandler<bool> ShowLoadingMessage;

        public ObservableCollection<TaskItem> Tasks { get; set; }

        // Property to hold the authorization URL previously handled in MainPage
        private string _authUrl;
        public string AuthUrl
        {
            get => _authUrl;
            private set => SetProperty(ref _authUrl, value);
        }

        // Expose the MonitorLocation so MainPage can display it if needed
        public string MonitorLocation => _netConfig?.MonitorLocation ?? "Unknown";

        private bool _isPolling;
        public bool IsPolling
        {
            get => _isPolling;
            set => SetProperty(ref _isPolling, value);
        }

        private bool _showToggle = true;
        public bool ShowToggle
        {
            get => _showToggle;
            set
            {
                SetProperty(ref _showToggle, value);
                SetProperty(ref _showTasks, value);
            }
        }

        private bool _showTasks = true;

        // Fields that mirror platform service properties
        private bool _isServiceStarted;
        private bool _disableAgentOnServiceShutdown;
        private string _serviceMessage = "No Service Message";
        private AgentUserFlow _agentUserFlow;

        public MainPageViewModel(NetConnectConfig netConfig, IPlatformService platformService, ILogger logger, IAuthService authService)
        {
            _netConfig = netConfig;
            _platformService = platformService;
            _logger = logger;
            _authService = authService;

            if (_platformService != null)
            {
                _platformService.ServiceStateChanged += PlatformServiceStateChanged;
                // Initialize local fields based on current platform state
                _isServiceStarted = _platformService.IsServiceStarted;
                _disableAgentOnServiceShutdown = _platformService.DisableAgentOnServiceShutdown;
                _serviceMessage = _platformService.ServiceMessage ?? "No Service Message";
            }
            else
            {
                _logger.LogError("_platformService is null in MainPageViewModel constructor.");
            }

            if (_netConfig?.AgentUserFlow != null)
            {
                _netConfig.AgentUserFlow.PropertyChanged += OnAgentUserFlowPropertyChanged;
                _agentUserFlow = _netConfig.AgentUserFlow;
            }
            else
            {
                _logger.LogError("_netConfig.AgentUserFlow is null in MainPageViewModel constructor.");
            }

            ToggleServiceCommand = new Command<bool>(async (value) => await SetServiceStartedAsync(value));
        }

        public bool ShowTasks
        {
            get
            {
                if (_disableAgentOnServiceShutdown) return _showTasks && _isServiceStarted;
                return _isServiceStarted;
            }
            set => SetProperty(ref _showTasks, value);
        }

        public string ServiceMessage
        {
            get => _serviceMessage;
            private set => SetProperty(ref _serviceMessage, value);
        }

        public void SetupTasks()
        {
            try
            {
                Action wrappedAuthorizeAction = () =>
                {
                    try
                    {
                        if (!IsPolling)
                        {
                            IsPolling = true;
                            AuthorizeAction?.Invoke(); // This will now call the ViewModel's AuthorizeAsync indirectly from MainPage
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error executing authorize action: {ex}");
                    }
                };

                Tasks = new ObservableCollection<TaskItem>
                {
                    new TaskItem
                    {
                        TaskDescription = "Authorize Agent",
                        IsCompleted = _agentUserFlow.IsAuthorized,
                        TaskAction = new Command(wrappedAuthorizeAction)
                    },
                    new TaskItem
                    {
                        TaskDescription = "Login Free Network Monitor",
                        IsCompleted = _agentUserFlow.IsLoggedInWebsite,
                        TaskAction = new Command(LoginAction)
                    },
                    new TaskItem
                    {
                        TaskDescription = "Scan for Hosts",
                        IsCompleted = _agentUserFlow.IsHostsAdded,
                        TaskAction = new Command(AddHostsAction)
                    }
                };
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in SetupTasks : {e.Message}");
            }
        }

        public async Task SetServiceStartedAsync(bool value)
        {
            try
            {
                await ChangeServiceAsync(value);

                if (_isServiceStarted && !value && _disableAgentOnServiceShutdown)
                {
                    ShowToggle = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error changing service state: {ex.Message}");
            }
        }

        private async Task ChangeServiceAsync(bool state)
        {
            try
            {
                ShowToggle = false;
                ShowLoadingMessage?.Invoke(this, true);
                await Task.Delay(200);
                await _platformService.ChangeServiceState(state);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in ChangeServiceAsync : {e.Message}");
            }
            finally
            {
                try
                {
                    _isServiceStarted = _platformService?.IsServiceStarted ?? false;
                    _disableAgentOnServiceShutdown = _platformService?.DisableAgentOnServiceShutdown ?? false;
                    _serviceMessage = _platformService?.ServiceMessage ?? "No Service Message";

                    ShowLoadingMessage?.Invoke(this, false);
                    ShowToggle = true;
                    OnPropertyChanged(nameof(ServiceMessage));
                    OnPropertyChanged(nameof(ShowTasks));
                    OnPropertyChanged(nameof(ShowToggle));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in ChangeServiceAsync : {ex.Message}");
                }
            }
        }

        private void PlatformServiceStateChanged(object? sender, EventArgs e)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _isServiceStarted = _platformService?.IsServiceStarted ?? false;
                    _disableAgentOnServiceShutdown = _platformService?.DisableAgentOnServiceShutdown ?? false;
                    _serviceMessage = _platformService?.ServiceMessage ?? "No Service Message";

                    OnPropertyChanged(nameof(ServiceMessage));
                    OnPropertyChanged(nameof(ShowTasks));
                    OnPropertyChanged(nameof(ShowToggle));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($" Error : handling service state change: {ex.Message}");
            }
        }

        private void OnAgentUserFlowPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(AgentUserFlow.IsAuthorized):
                            _agentUserFlow.IsAuthorized = _netConfig.AgentUserFlow.IsAuthorized;
                            UpdateTaskCompletion("Authorize Agent", _agentUserFlow.IsAuthorized);
                            break;
                        case nameof(AgentUserFlow.IsLoggedInWebsite):
                            _agentUserFlow.IsLoggedInWebsite = _netConfig.AgentUserFlow.IsLoggedInWebsite;
                            UpdateTaskCompletion("Login Free Network Monitor", _agentUserFlow.IsLoggedInWebsite);
                            break;
                        case nameof(AgentUserFlow.IsHostsAdded):
                            _agentUserFlow.IsHostsAdded = _netConfig.AgentUserFlow.IsHostsAdded;
                            UpdateTaskCompletion("Scan for Hosts", _agentUserFlow.IsHostsAdded);
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in OnAgentUserFlowPropertyChanged : {ex.Message}");
            }
        }

        public void UpdateTaskCompletion(string taskDescription, bool isCompleted)
        {
            try
            {
                var task = Tasks.FirstOrDefault(t => t.TaskDescription == taskDescription);
                if (task != null)
                {
                    task.IsCompleted = isCompleted;
                    OnPropertyChanged(nameof(Tasks));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating task completion for {taskDescription}: {ex.Message}");
            }
        }

        // New Methods that encapsulate logic originally in MainPage:

        public async Task<ResultObj> AuthorizeAsync()
        {
            var resultInit = await _authService.InitializeAsync();
            if (!resultInit.Success)
                return resultInit;

            var resultSend = await _authService.SendAuthRequestAsync();
            if (!resultSend.Success)
                return resultSend;

            AuthUrl = _netConfig.ClientAuthUrl;
            if (string.IsNullOrWhiteSpace(AuthUrl))
            {
                return new ResultObj { Success = false, Message = "Authorization URL is not available." };
            }

            // If AuthUrl is available, we can now poll for the token in background
            return new ResultObj { Success = true, Message = "Authorized successfully." };
        }

        public async Task<ResultObj> PollForTokenAsync(CancellationToken token)
        {
            var pollResult = await _authService.PollForTokenAsync(token);
            return pollResult;
        }

        public async Task<ResultObj> OpenLoginWebsiteAsync()
        {
            // Just return a successful result along with the URL
            return new ResultObj     { Success = true, Message = "https://freenetworkmonitor.click/dashboard" };
        }

        public async Task<ResultObj> ScanHostsAsync()
        {
            // Return the navigation route
            return new ResultObj { Success = true, Message = "//Scan" };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            try
            {
                if (Equals(storage, value))
                {
                    return false;
                }

                storage = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in SetProperty: {e.Message}");
                return false;
            }
        }
    }

    public class TaskItem : INotifyPropertyChanged
    {
        private bool _isCompleted;
        public string TaskDescription { get; set; } = "";
        public string ButtonText => IsCompleted ? $"{TaskDescription ?? "Task"} (Completed)" : TaskDescription ?? "Task";
        public Color ButtonBackgroundColor
        {
            get
            {
                Color color = Colors.White;
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (_isCompleted)
                        {
                            if (App.Current?.RequestedTheme == AppTheme.Dark)
                            {
                                color = ColorResource.GetResourceColor("Grey950");
                            }
                            else
                            {
                                color = Colors.White;
                            }
                        }
                        else
                        {
                            color = ColorResource.GetResourceColor("Warning");
                        }
                    });
                    return color;
                }
                catch
                {
                    return color;
                }
            }
        }

        public Color ButtonTextColor
        {
            get
            {
                try
                {
                    if (_isCompleted)
                    {
                        return ColorResource.GetResourceColor("Primary");
                    }
                    else { return Colors.White; }
                }
                catch
                {
                    return Colors.White;
                }
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _isCompleted = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(ButtonText));
                        OnPropertyChanged(nameof(ButtonBackgroundColor));
                        OnPropertyChanged(nameof(ButtonTextColor));
                    });
                }
                catch
                {
                }
            }
        }
        public ICommand TaskAction { get; set; }
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
