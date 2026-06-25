using MediatR;

// Domain events implement INotification so MediatR can dispatch them across module
// boundaries without any module knowing the concrete type of its subscribers.
// This is the foundation of our loosely coupled Modular Monolith architecture.
namespace HotelOS.SharedKernel.Abstractions;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
