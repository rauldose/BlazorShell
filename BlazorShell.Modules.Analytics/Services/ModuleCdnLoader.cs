// BlazorShell.Modules.Analytics/Services/ModuleCdnLoader.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Modules.Analytics.Services
{
    /// <summary>
    /// Manages CDN dependencies and module JavaScript for the Analytics module
    /// </summary>
    public class ModuleCdnLoader
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<ModuleCdnLoader>? _logger;
        private readonly List<CdnResource> _loadedResources = new();
        private bool _moduleFunctionsLoaded = false;
        private static readonly SemaphoreSlim _loadingSemaphore = new(1, 1);

        public ModuleCdnLoader(IJSRuntime jsRuntime, ILogger<ModuleCdnLoader>? logger = null)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        /// <summary>
        /// Define all CDN dependencies for the Analytics module
        /// </summary>
        private readonly List<CdnResource> RequiredResources = new()
        {
            new CdnResource
            {
                Name = "Chart.js",
                Type = ResourceType.JavaScript,
                Url = "https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js",
                FallbackUrl = "https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.0/chart.umd.min.js",
                IntegrityHash = null, // Add actual SRI hash if needed
                CrossOrigin = "anonymous",
                CheckLoaded = "typeof Chart !== 'undefined'",
                Order = 1
            },
            new CdnResource
            {
                Name = "date-fns",
                Type = ResourceType.JavaScript,
                Url = "https://cdn.jsdelivr.net/npm/date-fns@2.30.0/index.min.js",
                FallbackUrl = "https://unpkg.com/date-fns@2.30.0/index.min.js",
                CheckLoaded = "typeof dateFns !== 'undefined'",
                Order = 2
            },
            new CdnResource
            {
                Name = "chartjs-adapter-date-fns",
                Type = ResourceType.JavaScript,
                Url = "https://cdn.jsdelivr.net/npm/chartjs-adapter-date-fns@3.0.0/dist/chartjs-adapter-date-fns.bundle.min.js",
                CheckLoaded = "typeof Chart !== 'undefined' && Chart._adapters && Chart._adapters._date",
                Order = 3,
                DependsOn = new[] { "Chart.js", "date-fns" }
            }
        };

        /// <summary>
        /// Load all required resources (CDN libraries + module functions)
        /// </summary>
        public async Task<bool> LoadAllResourcesAsync()
        {
            await _loadingSemaphore.WaitAsync();
            try
            {
                // Check if already loaded
                if (_moduleFunctionsLoaded)
                {
                    _logger?.LogDebug("Resources already loaded");
                    return true;
                }

                _logger?.LogInformation("Loading CDN resources for Analytics module...");

                // Sort resources by dependencies
                var sortedResources = SortByDependencies(RequiredResources);

                // Load each CDN resource
                foreach (var resource in sortedResources)
                {
                    var loaded = await LoadResourceAsync(resource);
                    if (!loaded)
                    {
                        _logger?.LogError("Failed to load resource: {Resource}", resource.Name);
                        return false;
                    }
                }

                // After all CDN libraries are loaded, load our module functions
                var functionsLoaded = await LoadModuleFunctionsAsync();
                if (!functionsLoaded)
                {
                    _logger?.LogError("Failed to load module functions");
                    return false;
                }

                _logger?.LogInformation("All resources loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load resources");
                return false;
            }
            finally
            {
                _loadingSemaphore.Release();
            }
        }

        /// <summary>
        /// Load a single CDN resource with fallback support
        /// </summary>
        private async Task<bool> LoadResourceAsync(CdnResource resource)
        {
            try
            {
                // Check if already loaded
                if (!string.IsNullOrEmpty(resource.CheckLoaded))
                {
                    var isLoaded = await _jsRuntime.InvokeAsync<bool>("eval", resource.CheckLoaded);
                    if (isLoaded)
                    {
                        _logger?.LogDebug("{Resource} already loaded, skipping", resource.Name);
                        _loadedResources.Add(resource);
                        return true;
                    }
                }

                _logger?.LogDebug("Loading {Resource} from {Url}", resource.Name, resource.Url);

                // Try primary URL
                var loaded = await TryLoadFromUrlAsync(resource, resource.Url);

                // If failed and fallback exists, try fallback
                if (!loaded && !string.IsNullOrEmpty(resource.FallbackUrl))
                {
                    _logger?.LogWarning("Primary CDN failed for {Resource}, trying fallback", resource.Name);
                    loaded = await TryLoadFromUrlAsync(resource, resource.FallbackUrl);
                }

                if (loaded)
                {
                    _loadedResources.Add(resource);
                    _logger?.LogInformation("Successfully loaded {Resource}", resource.Name);
                }
                else
                {
                    _logger?.LogError("Failed to load {Resource} from all sources", resource.Name);
                }

                return loaded;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading resource {Resource}", resource.Name);
                return false;
            }
        }

        /// <summary>
        /// Try to load a resource from a specific URL
        /// </summary>
        private async Task<bool> TryLoadFromUrlAsync(CdnResource resource, string url)
        {
            if (resource.Type == ResourceType.JavaScript)
            {
                return await LoadJavaScriptAsync(resource, url);
            }
            else if (resource.Type == ResourceType.CSS)
            {
                return await LoadCssAsync(resource, url);
            }
            return false;
        }

        /// <summary>
        /// Load a JavaScript file from CDN
        /// </summary>
        private async Task<bool> LoadJavaScriptAsync(CdnResource resource, string url)
        {
            var script = $@"
                (function() {{
                    return new Promise((resolve, reject) => {{
                        // Check if already loading
                        window.__loadingScripts = window.__loadingScripts || {{}};
                        if (window.__loadingScripts['{url}']) {{
                            window.__loadingScripts['{url}'].then(resolve).catch(reject);
                            return;
                        }}
                        
                        // Create script element
                        const script = document.createElement('script');
                        script.src = '{url}';
                        script.type = 'text/javascript';
                        script.async = false;
                        script.dataset.module = 'analytics';
                        script.dataset.resource = '{resource.Name}';
                        
                        {(string.IsNullOrEmpty(resource.IntegrityHash) ? "" : $"script.integrity = '{resource.IntegrityHash}';")}
                        {(string.IsNullOrEmpty(resource.CrossOrigin) ? "" : $"script.crossOrigin = '{resource.CrossOrigin}';")}
                        
                        // Create loading promise
                        const loadPromise = new Promise((res, rej) => {{
                            script.onload = () => {{
                                console.log('✓ Loaded: {resource.Name}');
                                delete window.__loadingScripts['{url}'];
                                res(true);
                            }};
                            script.onerror = (error) => {{
                                console.error('✗ Failed to load: {resource.Name}', error);
                                delete window.__loadingScripts['{url}'];
                                script.remove();
                                rej(false);
                            }};
                        }});
                        
                        window.__loadingScripts['{url}'] = loadPromise;
                        document.head.appendChild(script);
                        
                        // Set timeout
                        setTimeout(() => {{
                            if (window.__loadingScripts['{url}']) {{
                                delete window.__loadingScripts['{url}'];
                                script.remove();
                                reject(new Error('Timeout loading script'));
                            }}
                        }}, 10000); // 10 second timeout
                        
                        loadPromise.then(resolve).catch(reject);
                    }});
                }})();
            ";

            try
            {
                var success = await _jsRuntime.InvokeAsync<bool>("eval", script);

                // Verify the resource actually loaded
                if (!string.IsNullOrEmpty(resource.CheckLoaded))
                {
                    await Task.Delay(100); // Small delay to ensure script executes
                    var isLoaded = await _jsRuntime.InvokeAsync<bool>("eval", resource.CheckLoaded);
                    return isLoaded;
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load JavaScript: {Resource}", resource.Name);
                return false;
            }
        }

        /// <summary>
        /// Load a CSS file from CDN
        /// </summary>
        private async Task<bool> LoadCssAsync(CdnResource resource, string url)
        {
            var script = $@"
                (function() {{
                    // Check if already loaded
                    const existing = document.querySelector('link[href=""{url}""]');
                    if (existing) {{
                        console.log('✓ CSS already loaded: {resource.Name}');
                        return true;
                    }}
                    
                    const link = document.createElement('link');
                    link.rel = 'stylesheet';
                    link.href = '{url}';
                    link.dataset.module = 'analytics';
                    link.dataset.resource = '{resource.Name}';
                    
                    {(string.IsNullOrEmpty(resource.IntegrityHash) ? "" : $"link.integrity = '{resource.IntegrityHash}';")}
                    {(string.IsNullOrEmpty(resource.CrossOrigin) ? "" : $"link.crossOrigin = '{resource.CrossOrigin}';")}
                    
                    document.head.appendChild(link);
                    console.log('✓ Added CSS: {resource.Name}');
                    return true;
                }})();
            ";

            try
            {
                return await _jsRuntime.InvokeAsync<bool>("eval", script);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load CSS: {Resource}", resource.Name);
                return false;
            }
        }

        /// <summary>
        /// Load module-specific JavaScript functions that depend on CDN libraries
        /// </summary>
        private async Task<bool> LoadModuleFunctionsAsync()
        {
            if (_moduleFunctionsLoaded)
            {
                _logger?.LogDebug("Module functions already loaded");
                return true;
            }

            _logger?.LogInformation("Loading Analytics module functions...");

            var moduleScript = @"
                (function() {
                    console.log('Initializing Analytics module functions...');
                    
                    // Initialize module namespace
                    window.AnalyticsModule = window.AnalyticsModule || {};
                    window.AnalyticsModule.charts = window.AnalyticsModule.charts || {};
                    
                    // Verify Chart.js is available
                    if (typeof Chart === 'undefined') {
                        console.error('Chart.js is not available');
                        return false;
                    }
                    
                    // Configure Chart.js defaults
                    Chart.defaults.font.family = '-apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif';
                    Chart.defaults.responsive = true;
                    Chart.defaults.maintainAspectRatio = false;
                    
                    // Define renderChart function
                    window.renderChart = function(canvasId, config) {
                        console.log('renderChart called for:', canvasId);
                        
                        const ctx = document.getElementById(canvasId);
                        if (!ctx) {
                            console.error('Canvas element not found:', canvasId);
                            return null;
                        }
                        
                        // Destroy existing chart
                        if (window.AnalyticsModule.charts[canvasId]) {
                            window.AnalyticsModule.charts[canvasId].destroy();
                            delete window.AnalyticsModule.charts[canvasId];
                        }
                        
                        // Create new chart
                        const chartConfig = {
                            type: config.type || 'line',
                            data: {
                                labels: config.labels || [],
                                datasets: [{
                                    label: config.label || 'Data',
                                    data: config.data || [],
                                    borderColor: config.borderColor || 'rgb(59, 130, 246)',
                                    backgroundColor: config.backgroundColor || 'rgba(59, 130, 246, 0.1)',
                                    borderWidth: config.borderWidth || 2,
                                    tension: config.tension || 0.4,
                                    fill: config.fill !== false
                                }]
                            },
                            options: {
                                responsive: true,
                                maintainAspectRatio: false,
                                animation: {
                                    duration: 750
                                },
                                plugins: {
                                    legend: {
                                        display: config.showLegend || false
                                    },
                                    tooltip: {
                                        enabled: true,
                                        mode: 'index',
                                        intersect: false
                                    }
                                },
                                scales: {
                                    y: {
                                        beginAtZero: true,
                                        ticks: {
                                            callback: function(value) {
                                                if (config.formatAsCurrency) {
                                                    return '$' + value.toLocaleString();
                                                }
                                                return value;
                                            }
                                        }
                                    }
                                }
                            }
                        };
                        
                        try {
                            window.AnalyticsModule.charts[canvasId] = new Chart(ctx.getContext('2d'), chartConfig);
                            console.log('✓ Chart created for:', canvasId);
                            return window.AnalyticsModule.charts[canvasId];
                        } catch (error) {
                            console.error('Error creating chart:', error);
                            return null;
                        }
                    };
                    
                    // Define renderPieChart function
                    window.renderPieChart = function(canvasId, config) {
                        console.log('renderPieChart called for:', canvasId);
                        
                        const ctx = document.getElementById(canvasId);
                        if (!ctx) {
                            console.error('Canvas element not found:', canvasId);
                            return null;
                        }
                        
                        // Destroy existing chart
                        if (window.AnalyticsModule.charts[canvasId]) {
                            window.AnalyticsModule.charts[canvasId].destroy();
                            delete window.AnalyticsModule.charts[canvasId];
                        }
                        
                        const chartConfig = {
                            type: config.type || 'doughnut',
                            data: {
                                labels: config.labels || [],
                                datasets: [{
                                    data: config.data || [],
                                    backgroundColor: config.colors || [
                                        '#3B82F6', '#10B981', '#F59E0B', '#8B5CF6', '#EF4444',
                                        '#06B6D4', '#84CC16', '#F97316', '#A855F7', '#EC4899'
                                    ],
                                    borderWidth: config.borderWidth || 0,
                                    borderColor: config.borderColor || '#fff'
                                }]
                            },
                            options: {
                                responsive: true,
                                maintainAspectRatio: false,
                                animation: {
                                    animateRotate: true,
                                    animateScale: false
                                },
                                plugins: {
                                    legend: {
                                        position: config.legendPosition || 'bottom',
                                        labels: {
                                            padding: 15,
                                            font: {
                                                size: 12
                                            }
                                        }
                                    },
                                    tooltip: {
                                        callbacks: {
                                            label: function(context) {
                                                const label = context.label || '';
                                                const value = context.parsed || 0;
                                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                                const percentage = ((value / total) * 100).toFixed(1);
                                                if (config.formatAsCurrency) {
                                                    return label + ': $' + value.toLocaleString() + ' (' + percentage + '%)';
                                                }
                                                return label + ': ' + value + ' (' + percentage + '%)';
                                            }
                                        }
                                    }
                                }
                            }
                        };
                        
                        try {
                            window.AnalyticsModule.charts[canvasId] = new Chart(ctx.getContext('2d'), chartConfig);
                            console.log('✓ Pie chart created for:', canvasId);
                            return window.AnalyticsModule.charts[canvasId];
                        } catch (error) {
                            console.error('Error creating pie chart:', error);
                            return null;
                        }
                    };
                    
                    // Define initializeLiveChart function
                    window.initializeLiveChart = function(chartId, config) {
                        console.log('initializeLiveChart called for:', chartId);
                        
                        const ctx = document.getElementById(chartId);
                        if (!ctx) {
                            console.error('Canvas element not found:', chartId);
                            return null;
                        }
                        
                        if (window.AnalyticsModule.charts[chartId]) {
                            window.AnalyticsModule.charts[chartId].destroy();
                            delete window.AnalyticsModule.charts[chartId];
                        }
                        
                        // Use provided config directly for more flexibility
                        try {
                            window.AnalyticsModule.charts[chartId] = new Chart(ctx.getContext('2d'), config);
                            console.log('✓ Live chart created for:', chartId);
                            return window.AnalyticsModule.charts[chartId];
                        } catch (error) {
                            console.error('Error creating live chart:', error);
                            return null;
                        }
                    };
                    
                    // Define updateLiveChart function
                    window.updateLiveChart = function(chartId, data) {
                        const chart = window.AnalyticsModule.charts[chartId];
                        if (!chart) {
                            console.warn('Chart not found:', chartId);
                            return false;
                        }
                        
                        try {
                            chart.data.labels = data.labels || [];
                            
                            if (data.datasets && Array.isArray(data.datasets)) {
                                data.datasets.forEach((dataset, index) => {
                                    if (chart.data.datasets[index]) {
                                        chart.data.datasets[index].data = dataset.data || [];
                                        // Update other properties if provided
                                        if (dataset.label) chart.data.datasets[index].label = dataset.label;
                                        if (dataset.borderColor) chart.data.datasets[index].borderColor = dataset.borderColor;
                                        if (dataset.backgroundColor) chart.data.datasets[index].backgroundColor = dataset.backgroundColor;
                                    }
                                });
                            }
                            
                            // Update with no animation for smooth real-time updates
                            chart.update('none');
                            return true;
                        } catch (error) {
                            console.error('Error updating chart:', error);
                            return false;
                        }
                    };
                    
                    // Define destroyChart function
                    window.destroyChart = function(chartId) {
                        if (window.AnalyticsModule.charts[chartId]) {
                            try {
                                window.AnalyticsModule.charts[chartId].destroy();
                                delete window.AnalyticsModule.charts[chartId];
                                console.log('✓ Chart destroyed:', chartId);
                                return true;
                            } catch (error) {
                                console.error('Error destroying chart:', error);
                                return false;
                            }
                        }
                        return false;
                    };
                    
                    // Define resetChartZoom function
                    window.resetChartZoom = function(chartId) {
                        const chart = window.AnalyticsModule.charts[chartId];
                        if (chart && chart.resetZoom) {
                            chart.resetZoom();
                            return true;
                        }
                        return false;
                    };
                    
                    // Define helper functions
                    window.showToast = function(message, type) {
                        type = type || 'info';
                        console.log(`Toast [${type}]: ${message}`);
                        
                        // Create toast element
                        const toast = document.createElement('div');
                        toast.className = 'analytics-toast';
                        toast.dataset.type = type;
                        
                        // Add styles if not present
                        if (!document.getElementById('analytics-toast-styles')) {
                            const style = document.createElement('style');
                            style.id = 'analytics-toast-styles';
                            style.textContent = `
                                .analytics-toast {
                                    position: fixed;
                                    top: 20px;
                                    right: 20px;
                                    background: white;
                                    border-radius: 8px;
                                    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                                    padding: 16px 20px;
                                    z-index: 999999;
                                    animation: slideIn 0.3s ease;
                                    display: flex;
                                    align-items: center;
                                    gap: 12px;
                                    min-width: 300px;
                                    max-width: 500px;
                                    border-left: 4px solid;
                                }
                                .analytics-toast[data-type='success'] { border-left-color: #10b981; }
                                .analytics-toast[data-type='error'] { border-left-color: #ef4444; }
                                .analytics-toast[data-type='warning'] { border-left-color: #f59e0b; }
                                .analytics-toast[data-type='info'] { border-left-color: #3b82f6; }
                                @keyframes slideIn {
                                    from { transform: translateX(400px); opacity: 0; }
                                    to { transform: translateX(0); opacity: 1; }
                                }
                                @keyframes slideOut {
                                    from { transform: translateX(0); opacity: 1; }
                                    to { transform: translateX(400px); opacity: 0; }
                                }
                            `;
                            document.head.appendChild(style);
                        }
                        
                        // Set content
                        const icons = {
                            success: '✓',
                            error: '✗',
                            warning: '⚠',
                            info: 'ℹ'
                        };
                        
                        toast.innerHTML = `
                            <div style='font-size: 20px; color: ${type === 'success' ? '#10b981' : type === 'error' ? '#ef4444' : type === 'warning' ? '#f59e0b' : '#3b82f6'};'>
                                ${icons[type] || icons.info}
                            </div>
                            <div style='flex: 1;'>${message}</div>
                        `;
                        
                        document.body.appendChild(toast);
                        
                        // Auto remove after 3 seconds
                        setTimeout(() => {
                            toast.style.animation = 'slideOut 0.3s ease';
                            setTimeout(() => toast.remove(), 300);
                        }, 3000);
                        
                        return true;
                    };
                    
                    window.playSound = function(type) {
                        console.log('Playing sound:', type);
                        // Sound implementation would go here
                        return true;
                    };
                    
                    // Clean up function
                    window.AnalyticsModule.dispose = function() {
                        console.log('Disposing Analytics module resources...');
                        
                        // Destroy all charts
                        if (window.AnalyticsModule.charts) {
                            Object.keys(window.AnalyticsModule.charts).forEach(chartId => {
                                window.destroyChart(chartId);
                            });
                        }
                        
                        // Remove styles
                        const styles = document.getElementById('analytics-toast-styles');
                        if (styles) styles.remove();
                        
                        // Clear namespace
                        window.AnalyticsModule.charts = {};
                        
                        console.log('✓ Analytics module disposed');
                        return true;
                    };
                    
                    console.log('✓ Analytics module functions loaded successfully');
                    window.AnalyticsModule.functionsLoaded = true;
                    return true;
                })();
            ";

            try
            {
                var success = await _jsRuntime.InvokeAsync<bool>("eval", moduleScript);

                if (!success)
                {
                    _logger?.LogError("Module script returned false");
                    return false;
                }

                // Verify critical functions exist
                var verificationScript = @"
                    typeof window.renderChart === 'function' && 
                    typeof window.renderPieChart === 'function' && 
                    typeof window.initializeLiveChart === 'function' &&
                    typeof window.updateLiveChart === 'function' &&
                    typeof window.AnalyticsModule === 'object' &&
                    window.AnalyticsModule.functionsLoaded === true
                ";

                var functionsExist = await _jsRuntime.InvokeAsync<bool>("eval", verificationScript);

                if (!functionsExist)
                {
                    _logger?.LogError("Module functions verification failed");
                    return false;
                }

                _moduleFunctionsLoaded = true;
                _logger?.LogInformation("Module functions loaded and verified successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load module functions");
                return false;
            }
        }

        /// <summary>
        /// Ensure all resources are loaded (quick check or full load)
        /// </summary>
        public async Task<bool> EnsureResourcesLoadedAsync()
        {
            // Quick check if already loaded
            if (_moduleFunctionsLoaded)
            {
                // Verify functions still exist (in case page was refreshed)
                try
                {
                    var stillLoaded = await _jsRuntime.InvokeAsync<bool>(
                        "eval",
                        "typeof window.renderChart === 'function' && window.AnalyticsModule && window.AnalyticsModule.functionsLoaded"
                    );

                    if (stillLoaded)
                    {
                        return true;
                    }

                    // Functions were cleared, need to reload
                    _moduleFunctionsLoaded = false;
                }
                catch
                {
                    _moduleFunctionsLoaded = false;
                }
            }

            // Load everything
            return await LoadAllResourcesAsync();
        }

        /// <summary>
        /// Unload all resources when module is deactivated
        /// </summary>
        public async Task UnloadResourcesAsync()
        {
            _logger?.LogInformation("Unloading CDN resources for Analytics module...");

            try
            {
                // Call dispose on module
                await _jsRuntime.InvokeVoidAsync("eval", @"
                    if (window.AnalyticsModule && window.AnalyticsModule.dispose) {
                        window.AnalyticsModule.dispose();
                    }
                ");

                // Remove script and link tags
                var cleanupScript = @"
                    (function() {
                        // Remove all scripts added by this module
                        const scripts = document.querySelectorAll('script[data-module=""analytics""]');
                        scripts.forEach(s => s.remove());
                        
                        // Remove all styles added by this module
                        const styles = document.querySelectorAll('link[data-module=""analytics""]');
                        styles.forEach(s => s.remove());
                        
                        // Clean up global functions (optional - might be used by other modules)
                        if (window.AnalyticsModule) {
                            delete window.renderChart;
                            delete window.renderPieChart;
                            delete window.initializeLiveChart;
                            delete window.updateLiveChart;
                            delete window.destroyChart;
                            delete window.resetChartZoom;
                            delete window.showToast;
                            delete window.playSound;
                            delete window.AnalyticsModule;
                        }
                        
                        console.log('✓ Analytics module resources unloaded');
                        return true;
                    })();
                ";

                await _jsRuntime.InvokeVoidAsync("eval", cleanupScript);

                _loadedResources.Clear();
                _moduleFunctionsLoaded = false;

                _logger?.LogInformation("Resources unloaded successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error unloading resources");
            }
        }

        /// <summary>
        /// Sort resources by their dependencies
        /// </summary>
        private List<CdnResource> SortByDependencies(List<CdnResource> resources)
        {
            var sorted = new List<CdnResource>();
            var visited = new HashSet<string>();

            void Visit(CdnResource resource)
            {
                if (visited.Contains(resource.Name)) return;

                if (resource.DependsOn != null)
                {
                    foreach (var dep in resource.DependsOn)
                    {
                        var depResource = resources.FirstOrDefault(r => r.Name == dep);
                        if (depResource != null)
                        {
                            Visit(depResource);
                        }
                    }
                }

                visited.Add(resource.Name);
                sorted.Add(resource);
            }

            // Process all resources ordered by their Order property first
            foreach (var resource in resources.OrderBy(r => r.Order))
            {
                Visit(resource);
            }

            return sorted;
        }
    }

    /// <summary>
    /// Represents a CDN resource to load
    /// </summary>
    public class CdnResource
    {
        public string Name { get; set; } = "";
        public ResourceType Type { get; set; }
        public string Url { get; set; } = "";
        public string? FallbackUrl { get; set; }
        public string? IntegrityHash { get; set; }
        public string? CrossOrigin { get; set; }
        public string? CheckLoaded { get; set; }
        public int Order { get; set; }
        public string[]? DependsOn { get; set; }
    }

    /// <summary>
    /// Type of resource
    /// </summary>
    public enum ResourceType
    {
        JavaScript,
        CSS
    }
}