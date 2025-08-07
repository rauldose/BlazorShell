using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BlazorShell.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace BlazorShell.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _storageRoot;

    public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        _storageRoot = Path.Combine(_environment.ContentRootPath, "Storage");

        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string? folder = null)
    {
        try
        {
            var folderPath = string.IsNullOrEmpty(folder)
                ? _storageRoot
                : Path.Combine(_storageRoot, folder);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            using var fileOutputStream = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(fileOutputStream);

            _logger.LogInformation("File saved: {FilePath}", filePath);

            return string.IsNullOrEmpty(folder)
                ? uniqueFileName
                : Path.Combine(folder, uniqueFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream?> GetFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_storageRoot, filePath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {FilePath}", fullPath);
                return null;
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return memory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file {FilePath}", filePath);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_storageRoot, filePath);

            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                _logger.LogInformation("File deleted: {FilePath}", fullPath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FilePath}", filePath);
            throw;
        }
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        var fullPath = Path.Combine(_storageRoot, filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task<IEnumerable<string>> GetFilesAsync(string folder)
    {
        try
        {
            var folderPath = string.IsNullOrEmpty(folder)
                ? _storageRoot
                : Path.Combine(_storageRoot, folder);

            if (!Directory.Exists(folderPath))
            {
                return Enumerable.Empty<string>();
            }

            var files = await Task.Run(() => Directory.GetFiles(folderPath));
            return files.Select(f => Path.GetRelativePath(_storageRoot, f));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get files from folder {Folder}", folder);
            throw;
        }
    }
}
