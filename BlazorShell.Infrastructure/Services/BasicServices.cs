using BlazorShell.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Infrastructure.Services
{
    /// <summary>
    /// Email service implementation
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var smtpUser = _configuration["Email:SmtpUser"];
                var smtpPassword = _configuration["Email:SmtpPassword"];
                var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@blazorshell.com";
                var fromName = _configuration["Email:FromName"] ?? "BlazorShell";

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(new MailAddress(to));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(smtpHost, smtpPort);

                if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPassword))
                {
                    client.Credentials = new NetworkCredential(smtpUser, smtpPassword);
                    client.EnableSsl = true;
                }

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw;
            }
        }

        public async Task SendTemplateEmailAsync(string to, string templateName, object model)
        {
            // This is a simplified implementation
            // In production, you would use a templating engine like RazorLight or similar

            var templatePath = Path.Combine("EmailTemplates", $"{templateName}.html");

            if (!File.Exists(templatePath))
            {
                _logger.LogWarning("Email template {Template} not found", templateName);
                throw new FileNotFoundException($"Email template {templateName} not found");
            }

            var templateContent = await File.ReadAllTextAsync(templatePath);

            // Simple token replacement - in production use a proper templating engine
            if (model != null)
            {
                var properties = model.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(model)?.ToString() ?? string.Empty;
                    templateContent = templateContent.Replace($"{{{{{prop.Name}}}}}", value);
                }
            }

            await SendEmailAsync(to, $"BlazorShell - {templateName}", templateContent);
        }
    }

    /// <summary>
    /// File storage service implementation
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _storageRoot;

        public FileStorageService(ILogger<FileStorageService> logger)
        {
            _logger = logger;
            _storageRoot = Path.Combine(Directory.GetCurrentDirectory(), "Storage");

            // Ensure storage directory exists
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

                // Generate unique file name to avoid conflicts
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var filePath = Path.Combine(folderPath, uniqueFileName);

                using var fileOutputStream = new FileStream(filePath, FileMode.Create);
                await fileStream.CopyToAsync(fileOutputStream);

                _logger.LogInformation("File saved: {FilePath}", filePath);

                // Return relative path
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

        public async Task<Stream> GetFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_storageRoot, filePath);

                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("File not found: {FilePath}", fullPath);
                    return Stream.Null;
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

        public async Task<bool> FileExistsAsync(string filePath)
        {
            var fullPath = Path.Combine(_storageRoot, filePath);
            return await Task.FromResult(File.Exists(fullPath));
        }
    }

    /// <summary>
    /// Cache service implementation using IMemoryCache
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                return await Task.FromResult(_cache.Get<T>(key));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache value for key {Key}", key);
                return default(T);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var cacheOptions = new MemoryCacheEntryOptions();

                if (expiration.HasValue)
                {
                    cacheOptions.SetAbsoluteExpiration(expiration.Value);
                }
                else
                {
                    // Default expiration of 1 hour
                    cacheOptions.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                }

                _cache.Set(key, value, cacheOptions);
                _logger.LogDebug("Cache set for key {Key}", key);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key {Key}", key);
                throw;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _cache.Remove(key);
                _logger.LogDebug("Cache removed for key {Key}", key);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache value for key {Key}", key);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await Task.FromResult(_cache.TryGetValue(key, out _));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key {Key}", key);
                return false;
            }
        }
    }
}