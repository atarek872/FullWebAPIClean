using Application.Common.Interfaces;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        // Implement email sending logic, e.g., using MailKit
        return Task.CompletedTask;
    }
}