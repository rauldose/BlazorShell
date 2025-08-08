using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlazorShell.Application.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

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
                : Path.GetFullPath(Path.Combine(_storageRoot, folder));

            if (!folderPath.StartsWith(_storageRoot, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid folder path");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            await using var fileOutputStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await fileStream.CopyToAsync(fileOutputStream).ConfigureAwait(false);

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
            var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, filePath));
            if (!fullPath.StartsWith(_storageRoot, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid file path");

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {FilePath}", fullPath);
                return null;
            }

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file {FilePath}", filePath);
            throw;
        }
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, filePath));
            if (!fullPath.StartsWith(_storageRoot, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid file path");

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {FilePath}", fullPath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FilePath}", filePath);
            throw;
        }
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, filePath));
        var exists = fullPath.StartsWith(_storageRoot, StringComparison.Ordinal) && File.Exists(fullPath);
        return Task.FromResult(exists);
    }

    public Task<IEnumerable<string>> GetFilesAsync(string folder)
    {
        try
        {
            var folderPath = string.IsNullOrEmpty(folder)
                ? _storageRoot
                : Path.GetFullPath(Path.Combine(_storageRoot, folder));

            if (!folderPath.StartsWith(_storageRoot, StringComparison.Ordinal) || !Directory.Exists(folderPath))
            {
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
            }

            var files = Directory.GetFiles(folderPath)
                .Select(f => Path.GetRelativePath(_storageRoot, f));
            return Task.FromResult<IEnumerable<string>>(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get files from folder {Folder}", folder);
            throw;
        }
    }
}
