namespace BlazorShell.Application.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendTemplatedEmailAsync(string to, string templateName, object model);
}
