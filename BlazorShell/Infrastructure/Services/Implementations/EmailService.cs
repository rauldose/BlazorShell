using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using BlazorShell.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
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
            message.IsBodyHtml = isHtml;

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

    public async Task SendTemplatedEmailAsync(string to, string templateName, object model)
    {
        var templatePath = Path.Combine("EmailTemplates", $"{templateName}.html");

        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Email template {Template} not found", templateName);
            throw new FileNotFoundException($"Email template {templateName} not found");
        }

        var templateContent = await File.ReadAllTextAsync(templatePath);

        if (model != null)
        {
            var properties = model.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(model)?.ToString() ?? string.Empty;
                templateContent = templateContent.Replace($"{{{{{prop.Name}}}}}", value);
            }
        }

        await SendEmailAsync(to, $"BlazorShell - {templateName}", templateContent, true);
    }
}
