// Components/DynamicRouteRefresher.cs
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorShell.Infrastructure.Services;
using BlazorShell.ModuleSystem.Services;

namespace BlazorShell.Components
{
    /// <summary>
    /// Component that provides JavaScript interop for route refreshing without page reload
    /// </summary>
    public class DynamicRouteRefresher : ComponentBase, IAsyncDisposable
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] private IDynamicRouteService DynamicRouteService { get; set; } = null!;
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;

        private IJSObjectReference? _jsModule;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/BlazorShell/dynamicRouting.js");

                await _jsModule.InvokeVoidAsync("initializeDynamicRouting");
            }
        }

        public async Task RefreshRoutesAsync()
        {
            if (_jsModule != null)
            {
                // Trigger a soft refresh that maintains state
                await _jsModule.InvokeVoidAsync("softRefreshRoutes");

                // Update the route service
                await DynamicRouteService.RefreshRoutesAsync();

                // Navigate to current URL to trigger re-render
                var currentUri = NavigationManager.Uri;
                NavigationManager.NavigateTo(currentUri, false, true);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_jsModule != null)
            {
                await _jsModule.DisposeAsync();
            }
        }
    }
}