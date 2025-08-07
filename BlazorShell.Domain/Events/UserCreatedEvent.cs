namespace BlazorShell.Domain.Events;

public class UserCreatedEvent : IDomainEvent
{
    public string UserId { get; }

    public UserCreatedEvent(string userId)
    {
        UserId = userId;
    }
}
