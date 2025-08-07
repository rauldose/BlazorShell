using BlazorShell.Domain.Events;
using Microsoft.Extensions.Logging;

namespace BlazorShell.Infrastructure.Events;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(ILogger<DomainEventDispatcher> logger)
    {
        _logger = logger;
    }

    public Task DispatchAsync(IDomainEvent domainEvent)
    {
        _logger.LogInformation("Domain event dispatched: {EventType}", domainEvent.GetType().Name);
        return Task.CompletedTask;
    }
}
