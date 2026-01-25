namespace Application.Common.Interfaces;

public interface ISmsService
{
    Task SendSmsAsync(string to, string message, CancellationToken cancellationToken = default);
}