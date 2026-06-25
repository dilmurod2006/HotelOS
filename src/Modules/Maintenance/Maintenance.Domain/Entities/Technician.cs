using HotelOS.SharedKernel.Abstractions;

namespace Maintenance.Domain.Entities;

/// <summary>
/// Entity: Technician.
/// The Priority Queue Algorithm assigns the next open issue to the next available technician.
/// Availability is tracked here — a technician with IsAvailable = false is skipped by the dispatcher.
/// </summary>
public sealed class Technician : AggregateRoot<Guid>
{
    public string FullName { get; private set; } = null!;
    public bool IsAvailable { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Technician() { }

    public static Technician Create(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Technician name is required.");

        return new Technician
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkBusy() => IsAvailable = false;
    public void MarkAvailable() => IsAvailable = true;
}
