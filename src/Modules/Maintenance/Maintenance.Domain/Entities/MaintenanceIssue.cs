using HotelOS.SharedKernel.Abstractions;
using Maintenance.Domain.Enums;
using Maintenance.Domain.Events;

namespace Maintenance.Domain.Entities;

/// <summary>
/// Aggregate Root: MaintenanceIssue.
///
/// Represents a technical problem reported for a specific room.
/// The Priority Queue Algorithm is implemented by the service layer that reads these
/// entities — issues are sorted by (Urgency ASC, CreatedAt ASC). The domain entity
/// itself exposes the Urgency and CreatedAt fields that drive that ordering.
///
/// Critical issues automatically trigger Room.PlaceUnderMaintenance() via a domain event,
/// making the room unavailable until the issue is resolved.
/// </summary>
public sealed class MaintenanceIssue : AggregateRoot<Guid>
{
    public Guid RoomId { get; private set; }
    public string RoomNumber { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public IssueUrgency Urgency { get; private set; }
    public IssueStatus Status { get; private set; }
    public Guid? AssignedTechnicianId { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private MaintenanceIssue() { }

    public static MaintenanceIssue Report(
        Guid roomId,
        string roomNumber,
        string description,
        IssueUrgency urgency)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Length > 500)
            throw new ArgumentException("Description must be 1–500 characters.");

        var issue = new MaintenanceIssue
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            RoomNumber = roomNumber,
            Description = description.Trim(),
            Urgency = urgency,
            Status = IssueStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Critical issues immediately lock the room — no bookings allowed until resolved.
        if (urgency == IssueUrgency.Critical)
            issue.RaiseDomainEvent(new CriticalIssueReportedEvent(issue.Id, roomId, roomNumber));

        issue.RaiseDomainEvent(new IssueStatusChangedEvent(issue.Id, roomNumber, IssueStatus.Open, urgency));

        return issue;
    }

    /// <summary>
    /// Priority Queue Algorithm feeds technician IDs here.
    /// The next available technician is determined externally (FIFO among free technicians),
    /// then assigned here.
    /// </summary>
    public void AssignToTechnician(Guid technicianId)
    {
        if (Status != IssueStatus.Open)
            throw new InvalidOperationException("Only Open issues can be assigned.");

        AssignedTechnicianId = technicianId;
        Status = IssueStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new IssueStatusChangedEvent(Id, RoomNumber, Status, Urgency));
    }

    public void StartWork()
    {
        if (Status != IssueStatus.Assigned)
            throw new InvalidOperationException("Issue must be Assigned before work can start.");

        Status = IssueStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new IssueStatusChangedEvent(Id, RoomNumber, Status, Urgency));
    }

    /// <summary>
    /// Resolving a Critical issue fires an event so Reception can return the room to service.
    /// </summary>
    public void Resolve(string resolutionNotes)
    {
        if (Status == IssueStatus.Resolved)
            throw new InvalidOperationException("Issue is already resolved.");

        ResolutionNotes = resolutionNotes;
        Status = IssueStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new IssueStatusChangedEvent(Id, RoomNumber, Status, Urgency));

        if (Urgency == IssueUrgency.Critical)
            RaiseDomainEvent(new CriticalIssueResolvedEvent(Id, RoomId, RoomNumber));
    }
}
