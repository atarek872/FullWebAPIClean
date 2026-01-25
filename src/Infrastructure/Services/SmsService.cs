using Application.Common.Interfaces;

namespace Infrastructure.Services;

public class SmsService : ISmsService
{
    public Task SendSmsAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        // Implement SMS sending logic
        return Task.CompletedTask;
    }
}