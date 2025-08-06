// Core/Components/ImprovedModuleComponentBase.cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using BlazorShell.Core.Interfaces;
using BlazorShell.Infrastructure.Services;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using BlazorShell.Core.Entities;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Core.Components
{
    public abstract class ModuleComponentBase : ComponentBase, IDisposable
    {
        private IServiceScope? _serviceScope;
        private bool _servicesInitialized = false;
        private ILogger? _logger;

        [Inject] protected IServiceProvider ServiceProvider { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] protected IModuleAuthorizationService? ModuleAuth { get; set; }
        [Inject] protected IStateContainer? StateContainer { get; set; }

        // Fallback to old provider
        [Inject] protected IModuleServiceProvider? ModuleServiceProvider { get; set; }

        protected string? UserId { get; private set; }
        protected bool IsAuthenticated { get; private set; }
        protected ClaimsPrincipal? User { get; private set; }

        protected virtual string? ModuleName => GetType().Namespace?.Split('.')[2]; // Extract module name from namespace

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Initialize logger first
                InitializeLogger();

                // Create a service scope that includes module services
                if (!string.IsNullOrEmpty(ModuleName))
                {
                    _logger?.LogDebug("Initializing component for module: {Module}", ModuleName);

                    var scopeFactory = ServiceProvider.GetService<IServiceScopeFactory>();
                    if (scopeFactory != null)
                    {
                        _serviceScope = scopeFactory.CreateScope();
                    }

                    // Ensure module services are registered
                    EnsureModuleServicesRegistered();
                }

                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                User = authState.User;
                IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
                UserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (IsAuthenticated && !string.IsNullOrEmpty(ModuleName) && ModuleAuth != null)
                {
                    if (!await ModuleAuth.CanAccessModuleAsync(UserId!, ModuleName))
                    {
                        _logger?.LogWarning("User {UserId} does not have access to module {Module}", UserId, ModuleName);
                        Navigation.NavigateTo("/unauthorized");
                        return;
                    }
                }

                _servicesInitialized = true;
                await OnModuleInitializedAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing module component for module {Module}", ModuleName);
                throw;
            }
        }

        private void InitializeLogger()
        {
            try
            {
                var loggerFactory = ServiceProvider.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                {
                    _logger = loggerFactory.CreateLogger(GetType());
                }
            }
            catch
            {
                // Logging not available
            }
        }

        private void EnsureModuleServicesRegistered()
        {
            if (string.IsNullOrEmpty(ModuleName))
                return;

            try
            {
                // Check if module services are registered
                var provider = ModuleServiceProvider ?? ModuleServiceProvider as IModuleServiceProvider;

                if (provider != null && !provider.IsModuleRegistered(ModuleName))
                {
                    _logger?.LogWarning("Module {Module} services are not registered. This might cause service resolution issues.", ModuleName);

                    // Try to get the module and register its services
                    var moduleRegistry = ServiceProvider.GetService<IModuleRegistry>();
                    var module = moduleRegistry?.GetModule(ModuleName);

                    if (module is IServiceModule serviceModule && provider != null)
                    {
                        _logger?.LogInformation("Auto-registering services for module {Module}", ModuleName);
                        provider.RegisterModuleServices(ModuleName, serviceModule);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error ensuring module services are registered for {Module}", ModuleName);
            }
        }

        protected virtual Task OnModuleInitializedAsync() => Task.CompletedTask;

        protected T? GetService<T>() where T : class
        {
            if (!_servicesInitialized)
            {
                _logger?.LogWarning("Attempting to get service {ServiceType} before initialization completed", typeof(T).Name);
            }

            try
            {
                // Try improved module service provider first
                if (ModuleServiceProvider != null)
                {
                    var service = ModuleServiceProvider.GetService<T>();
                    if (service != null)
                    {
                        _logger?.LogDebug("Resolved service {ServiceType} from improved module provider", typeof(T).Name);
                        return service;
                    }
                }

                // Try old module service provider
                if (ModuleServiceProvider != null)
                {
                    var service = ModuleServiceProvider.GetService<T>();
                    if (service != null)
                    {
                        _logger?.LogDebug("Resolved service {ServiceType} from module provider", typeof(T).Name);
                        return service;
                    }
                }

                // Try scoped services
                if (_serviceScope != null)
                {
                    var service = _serviceScope.ServiceProvider.GetService<T>();
                    if (service != null)
                    {
                        _logger?.LogDebug("Resolved service {ServiceType} from scoped provider", typeof(T).Name);
                        return service;
                    }
                }

                // Fall back to root services
                var rootService = ServiceProvider.GetService<T>();
                if (rootService != null)
                {
                    _logger?.LogDebug("Resolved service {ServiceType} from root provider", typeof(T).Name);
                    return rootService;
                }

                _logger?.LogWarning("Could not resolve service {ServiceType} from any provider", typeof(T).Name);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error resolving service {ServiceType}", typeof(T).Name);
                return null;
            }
        }

        protected T GetRequiredService<T>() where T : class
        {
            var service = GetService<T>();
            if (service == null)
            {
                var message = $"Service of type {typeof(T).Name} is not registered for module {ModuleName}";
                _logger?.LogError(message);
                throw new InvalidOperationException(message);
            }
            return service;
        }

        protected async Task<bool> HasPermissionAsync(PermissionType permission)
        {
            if (string.IsNullOrEmpty(ModuleName) || string.IsNullOrEmpty(UserId) || ModuleAuth == null)
                return false;

            return await ModuleAuth.HasPermissionAsync(UserId, ModuleName, permission);
        }

        protected void NavigateTo(string url, bool forceLoad = false)
        {
            Navigation.NavigateTo(url, forceLoad);
        }

        protected void RefreshPage()
        {
            Navigation.Refresh();
        }

        public virtual void Dispose()
        {
            _serviceScope?.Dispose();
        }
    }
}