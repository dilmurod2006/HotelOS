using HotelOS.SharedKernel.Abstractions;

namespace HotelOS.SharedKernel.Common;

// BaseEvent provides the standard event envelope that every module's domain
// events inherit from. The EventId ensures idempotency in event handlers.
public abstract record BaseEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
