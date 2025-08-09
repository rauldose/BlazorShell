using BlazorShell.Modules.Analytics.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

public class CdnResourceManager : ICdnResourceManager
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<CdnResourceManager> _logger;
    private ModuleCdnLoader? _cdnLoader;
    private bool _resourcesLoaded = false;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public CdnResourceManager(IJSRuntime jsRuntime, ILogger<CdnResourceManager> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public bool AreResourcesLoaded => _resourcesLoaded;

    public async Task<bool> EnsureResourcesLoadedAsync()
    {
        if (_resourcesLoaded)
        {
            return true;
        }

        await _loadLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_resourcesLoaded)
            {
                return true;
            }

            // Create loader if needed
            if (_cdnLoader == null)
            {
                _cdnLoader = new ModuleCdnLoader(_jsRuntime, _logger as ILogger<ModuleCdnLoader>);
            }

            // Load resources
            var loaded = await _cdnLoader.LoadAllResourcesAsync();

            if (loaded)
            {
                _resourcesLoaded = true;
                _logger.LogInformation("CDN resources loaded successfully");
            }
            else
            {
                _logger.LogWarning("Failed to load CDN resources");
            }

            return loaded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading CDN resources");
            return false;
        }
        finally
        {
            _loadLock.Release();
        }
    }
}