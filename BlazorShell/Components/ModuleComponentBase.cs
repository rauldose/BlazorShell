// Core/Components/ModuleComponentBase.cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using BlazorShell.Core.Interfaces;
using BlazorShell.Infrastructure.Services;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using BlazorShell.Core.Entities;

namespace BlazorShell.Core.Components
{
    public abstract class ModuleComponentBase : ComponentBase, IDisposable
    {
        private IServiceScope? _serviceScope;

        [Inject] protected IServiceProvider ServiceProvider { get; set; } = default!;
        [Inject] protected IModuleServiceProvider ModuleServiceProvider { get; set; } = default!;
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] protected IModuleAuthorizationService? ModuleAuth { get; set; }
        [Inject] protected IStateContainer? StateContainer { get; set; }

        protected string? UserId { get; private set; }
        protected bool IsAuthenticated { get; private set; }
        protected ClaimsPrincipal? User { get; private set; }

        protected virtual string? ModuleName => GetType().Namespace?.Split('.')[2]; // Extract module name from namespace

        protected override async Task OnInitializedAsync()
        {
            // Create a service scope that includes module services
            if (!string.IsNullOrEmpty(ModuleName))
            {
                var scopeFactory = ServiceProvider.GetService<IServiceScopeFactory>();
                if (scopeFactory != null)
                {
                    _serviceScope = scopeFactory.CreateScope();
                }
            }

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            User = authState.User;
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
            UserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (IsAuthenticated && !string.IsNullOrEmpty(ModuleName) && ModuleAuth != null)
            {
                if (!await ModuleAuth.CanAccessModuleAsync(UserId!, ModuleName))
                {
                    Navigation.NavigateTo("/unauthorized");
                    return;
                }
            }

            await OnModuleInitializedAsync();
        }

        protected virtual Task OnModuleInitializedAsync() => Task.CompletedTask;

        protected T? GetService<T>() where T : class
        {
            // Try module services first
            var service = ModuleServiceProvider.GetService<T>();
            if (service != null)
            {
                return service;
            }

            // Try scoped services
            if (_serviceScope != null)
            {
                service = _serviceScope.ServiceProvider.GetService<T>();
                if (service != null)
                {
                    return service;
                }
            }

            // Fall back to root services
            return ServiceProvider.GetService<T>();
        }

        protected T GetRequiredService<T>() where T : class
        {
            var service = GetService<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered");
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