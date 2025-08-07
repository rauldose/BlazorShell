using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Modules.Admin.Services;

public interface IModuleUploadService
{
    Task<UploadResult> UploadModuleAsync(IBrowserFile file, IProgress<int>? progress = null);
    bool ValidateModuleFile(IBrowserFile file);
}

public class UploadResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ModuleName { get; set; }
}

public class ModuleUploadService : IModuleUploadService
{
    private readonly ILogger<ModuleUploadService> _logger;
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB
    private readonly string[] _allowedExtensions = { ".dll", ".zip" };

    public ModuleUploadService(ILogger<ModuleUploadService> logger)
    {
        _logger = logger;
    }

    public bool ValidateModuleFile(IBrowserFile file)
    {
        if (file == null)
        {
            _logger.LogWarning("No file provided for validation");
            return false;
        }

        if (file.Size > MaxFileSize)
        {
            _logger.LogWarning("File size {Size} exceeds maximum allowed size {MaxSize}", file.Size, MaxFileSize);
            return false;
        }

        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            _logger.LogWarning("File extension {Extension} is not allowed", extension);
            return false;
        }

        _logger.LogInformation("File {FileName} validation passed", file.Name);
        return true;
    }

    public async Task<UploadResult> UploadModuleAsync(IBrowserFile file, IProgress<int>? progress = null)
    {
        try
        {
            if (!ValidateModuleFile(file))
            {
                return new UploadResult 
                { 
                    Success = false, 
                    Message = "File validation failed. Check file size and extension." 
                };
            }

            _logger.LogInformation("Starting upload of module file: {FileName}", file.Name);

            // Create temporary directory for upload
            var tempDir = Path.Combine(Path.GetTempPath(), "BlazorShell", "Uploads");
            Directory.CreateDirectory(tempDir);

            var tempFile = Path.Combine(tempDir, $"{Guid.NewGuid()}_{file.Name}");

            // Upload file with progress tracking
            using var stream = file.OpenReadStream(MaxFileSize);
            using var fileStream = new FileStream(tempFile, FileMode.Create);

            var buffer = new byte[8192];
            var totalRead = 0L;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                var progressPercentage = (int)((totalRead * 100) / file.Size);
                progress?.Report(progressPercentage);
            }

            _logger.LogInformation("Module file uploaded successfully to: {TempFile}", tempFile);

            // Here you would typically:
            // 1. Validate the module assembly
            // 2. Extract metadata
            // 3. Install the module
            // For now, we'll simulate this process

            await Task.Delay(1000); // Simulate processing time
            progress?.Report(100);

            // Clean up temp file
            File.Delete(tempFile);

            var moduleName = Path.GetFileNameWithoutExtension(file.Name);
            
            return new UploadResult
            {
                Success = true,
                Message = $"Module '{moduleName}' uploaded successfully.",
                ModuleName = moduleName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading module file: {FileName}", file.Name);
            return new UploadResult
            {
                Success = false,
                Message = $"Upload failed: {ex.Message}"
            };
        }
    }
}